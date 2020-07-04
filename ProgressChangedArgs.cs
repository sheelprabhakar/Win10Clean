using System;

namespace Win10Clean
{
    public class ProgressChangedArgs : EventArgs
    {
        public string Progress { get; private set; }
        public ProgressChangedArgs(string progress)
        {
            Progress = progress;
        }
    }
}
