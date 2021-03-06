﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed class QuickInfoController : IIntellisenseController {
		readonly IQuickInfoBroker quickInfoBroker;
		readonly ITextView textView;

		public QuickInfoController(IQuickInfoBroker quickInfoBroker, ITextView textView) {
			if (quickInfoBroker == null)
				throw new ArgumentNullException(nameof(quickInfoBroker));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			this.quickInfoBroker = quickInfoBroker;
			textView.MouseHover += TextView_MouseHover;
		}

		public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }
		public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }

		void TextView_MouseHover(object sender, MouseHoverEventArgs e) {
			var pos = e.TextPosition.GetPoint(textView.TextBuffer, PositionAffinity.Successor);
			if (pos == null)
				return;
			var sessions = quickInfoBroker.GetSessions(textView);
			foreach (var session in sessions) {
				if (Intersects(session.ApplicableToSpan, pos.Value))
					return;
				if ((session as IQuickInfoSession2)?.HasInteractiveContent == true) {
					foreach (var o in session.QuickInfoContent) {
						var io = o as IInteractiveQuickInfoContent;
						if (io == null)
							continue;
						if (io.KeepQuickInfoOpen || io.IsMouseOverAggregated)
							return;
					}
				}
			}
			foreach (var session in sessions)
				session.Dismiss();
			var triggerPoint = pos.Value.Snapshot.CreateTrackingPoint(pos.Value.Position, PointTrackingMode.Negative);
			quickInfoBroker.TriggerQuickInfo(textView, triggerPoint, trackMouse: true);
		}

		bool Intersects(ITrackingSpan span, SnapshotPoint point) {
			if (span == null)
				return false;
			if (point.Snapshot.TextBuffer != span.TextBuffer)
				return false;
			var span2 = span.GetSpan(span.TextBuffer.CurrentSnapshot);
			return span2.IntersectsWith(new SnapshotSpan(point, 0));
		}

		public void Detach(ITextView textView) => textView.MouseHover -= TextView_MouseHover;
	}
}
