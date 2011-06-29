using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Data;

namespace iBoard.Classes.Timeline {
    public abstract class TimelineListener : ITimelineUpdateEventListener {

        /// <summary>
        /// Instanciates a new TimelineListener and add it as a timeline listener
        /// </summary>
        public TimelineListener() {
            TimelineManager.AddTimelineListenner(this);
        }

        /// <summary>
        /// Should be implemented by the childs
        /// </summary>
        public abstract void TimelineUpdate();

        /// <summary>
        /// Adds a frame to the timeline
        /// </summary>
        /// <param name="frame">Frame instance to add</param>
        public virtual Boolean AddFrame(Frame frame) {
            return TimelineManager.AddFrame(frame);
        }
    }
}
