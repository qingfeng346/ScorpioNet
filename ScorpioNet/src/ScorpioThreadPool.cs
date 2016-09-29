using System.Threading;

namespace Scorpio.Net {
    public class ScorpioThreadPool {
        public delegate void ScorpioThreadHandler();
        public static void CreateThread(ScorpioThreadHandler handler) {
            ThreadPool.QueueUserWorkItem(_ => { handler(); });
        }
    }
}
