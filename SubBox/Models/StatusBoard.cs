using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SubBox.Models
{
    public class StatusBoard
    {
        private static List<StatusUpdate> Board = new List<StatusUpdate>();

        private static SemaphoreSlim Locker = new SemaphoreSlim(1, 1);

        public async static Task<StatusUpdate> GetStatus(string kind, string key)
        {
            await Locker.WaitAsync();

            try
            {
                StatusUpdate status = Board.FirstOrDefault((s) => ((s.Kind == kind) && (s.Key == key)));

                if (status != null)
                {
                    Board.Remove(status);

                    return status;
                }

                return new StatusUpdate()
                {
                    Kind = "noStatus",

                    Key = "",

                    Value = ""
                };
            }
            finally
            {
                Locker.Release();
            }
        }

        public async static Task PutStatus(StatusUpdate status)
        {
            await Locker.WaitAsync();

            try
            {
                Board.Add(status);
            }
            finally 
            {
                Locker.Release();
            }
        }

        public async static Task PutStatus(string kind, string key, string value)
        {
            await PutStatus(new StatusUpdate()
            {
                Kind = kind,

                Key = key,

                Value = value
            });
        }

        public async static Task PrintAllStatus()
        {
            await Locker.WaitAsync();

            try
            {
                foreach(StatusUpdate s in Board)
                {
                    Logger.Info("Kind: " + s.Kind + ", Key: " + s.Key + ", Value: " + s.Value);
                }
            }
            finally
            {
                Locker.Release();
            }
        }

    }
}
