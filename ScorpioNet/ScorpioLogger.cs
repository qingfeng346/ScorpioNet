namespace Scorpio.Net {
    public interface ILog {
        /// <summary> debug 信息 </summary>
        void debug(string format);
        /// <summary> info 信息 </summary>
        void info(string format);
        /// <summary> 警告 信息 </summary>
        void warn(string format);
        /// <summary> 错误 信息 </summary>
        void error(string format);
    }
    public static class logger {
        private static ILog log = null;
        /// <summary> 设置日志对象 </summary>
        public static void SetLog(ILog ilog) {
            log = ilog;
        }
        /// <summary> debug输出 </summary>
        public static void debug(string format) {
            if (log == null) return;
            log.debug(format);
        }
        /// <summary> info输出 </summary>
        public static void info(string format) {
            if (log == null) return;
            log.info(format);
        }
        /// <summary> warn输出 </summary>
        public static void warn(string format) {
            if (log == null) return;
            log.warn(format);
        }
        /// <summary> error输出 </summary>
        public static void error(string format) {
            if (log == null) return;
            log.error(format);
        }
    }
}
