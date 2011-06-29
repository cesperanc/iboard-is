using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iBoard.Classes.Timeline;

namespace iBoard.Controls {
    /// <summary>
    /// Interaction logic for TimelineViewer.xaml
    /// </summary>
    public partial class TimelineViewer : UserControl, IStatusUpdateEventListener {
        private MainWindow _parentWindow = null;

        public TimelineViewer() {
            InitializeComponent();
            this._init();
        }

        /// <summary>
        /// Init the control
        /// </summary>
        private void _init() {
            StatusManager.AddStatusListenner(this);
            // Handler for loading complete
            this.Loaded += (se, args) => {
                try {
                    this._parentWindow = (MainWindow) Window.GetWindow(this);
                    if(this._parentWindow != null) {
                        this._parentWindow.lblControlLoading.Visibility = System.Windows.Visibility.Hidden;
                        TimelineManager.Update();
                    }
                } catch(Exception) {
                    // this shouldn't happen
                }
            };
        }

        public void StatusUpdate(Status status) {
            expStatus.Header = status.Name + ": " + status.Description;
        }
    }
}
