using System;

namespace Pulsar.Client.Helper.HVNC
{
    /// <summary>
    /// Represents the progress state while cloning a browser profile directory.
    /// </summary>
    internal readonly struct BrowserCloneProgress
    {
        public BrowserCloneProgress(int filesCopied, int totalFiles, string currentItem, bool isIndeterminate = false)
        {
            FilesCopied = filesCopied;
            TotalFiles = totalFiles;
            CurrentItem = currentItem ?? string.Empty;
            IsIndeterminate = isIndeterminate;
        }

        /// <summary>
        /// Gets the number of files copied so far.
        /// </summary>
        public int FilesCopied { get; }

        /// <summary>
        /// Gets the total number of files scheduled for cloning.
        /// </summary>
        public int TotalFiles { get; }

        /// <summary>
        /// Gets the relative path of the item currently being cloned.
        /// </summary>
        public string CurrentItem { get; }

        /// <summary>
        /// Indicates whether the operation is currently in an indeterminate state.
        /// </summary>
        public bool IsIndeterminate { get; }

        /// <summary>
        /// Gets the progress percentage (0-100) when the total file count is known.
        /// </summary>
        public int Percent
        {
            get
            {
                if (TotalFiles <= 0)
                {
                    return 0;
                }

                double raw = (double)FilesCopied / TotalFiles * 100d;
                if (raw < 0d)
                {
                    return 0;
                }

                if (raw > 100d)
                {
                    return 100;
                }

                return (int)Math.Round(raw);
            }
        }
    }
}
