using System;
using System.Collections.Generic;
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
using iBoard.Classes.Data;


namespace iBoard
{
	/// <summary>
	/// Interaction logic for TwitterControl.xaml
	/// </summary>
    public partial class MoodleControl : UserControl, IStatusUpdateEventListener {
        private MainWindow _parentWindow = null;
        private int _accountId = -1;

        public MoodleControl(int accountId) {
            this.InitializeComponent();
            this._accountId = accountId;
            this._init();
		}
		
		/// <summary>
        /// Init the control
        /// </summary>
        private void _init() {
            ListCollectionView myCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(gridBase.DataContext);
            myCollectionView.Filter = _filterMoodleAccounts;
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

        /// <summary>
        /// Update the status element
        /// </summary>
        /// <param name="status"></param>
        public void StatusUpdate(Status status) {
            expStatus.Header = status.Name + ": " + status.Description;
        }

        /// <summary>
        /// Filter all frames out the don't belong to this account
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool _filterMoodleAccounts(object item) {
            iBoard.Classes.Timeline.Frame account = item as iBoard.Classes.Timeline.Frame;

            return (account.FrameAccount.ID == this._accountId);
        }
    }
}