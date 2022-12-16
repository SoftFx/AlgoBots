using System;
using System.Text;

namespace SoftFx
{
    public interface IConfig
    {
        void Init();
    }


    public class BotConfig : IConfig
    {
        private StringBuilder _errorsBuilder;
        private StringBuilder _warningsBuilder;

        public BotConfig()
        {
            _errorsBuilder = new StringBuilder(1 << 8);
            _warningsBuilder = new StringBuilder(1 << 8);
        }

        public virtual void Init() { }

        public void PrintError(string error)
        {
            _errorsBuilder.AppendLine(error);
            PrintErrorEvent?.Invoke(error);
        }

        public void PrintWarning(string warning)
        {
            _warningsBuilder.AppendLine(warning);
            PrintWarningEvent?.Invoke(warning);
        }

        [Nett.TomlIgnore]
        public bool HasErrors => _errorsBuilder.Length > 0;

        [Nett.TomlIgnore]
        public bool HasWarnings => _warningsBuilder.Length > 0;

        [Nett.TomlIgnore]
        public string GetAllErrors => _errorsBuilder.ToString();

        [Nett.TomlIgnore]
        public string GetAllWarnings => _warningsBuilder.ToString();

        public event Action<string> PrintErrorEvent;
        public event Action<string> PrintWarningEvent;


        protected static class Rule
        {
            public static bool CheckNumberGt<T>(string name, T value, T border) where T : IComparable =>
                value.CompareTo(border) == 1 || ThrowException(name, value, $"should be greater than {border}");

            public static bool CheckNumberGte<T>(string name, T value, T border) where T : IComparable =>
                value.CompareTo(border) != -1 || ThrowException(name, value, $"should be greater or equals than {border}");

            public static bool CheckNumberLte<T>(string name, T value, T border) where T : IComparable =>
                value.CompareTo(border) != 1 || ThrowException(name, value, $"should be less or equals than {border}");

            public static bool CheckNumberInRange<T>(string name, T value, T lowBorder, T highBorder) where T : IComparable
            {
                var result = value.CompareTo(lowBorder) != -1 && value.CompareTo(highBorder) != 1;

                return result ? result : ThrowException(name, value, $"should be between [{lowBorder}..{highBorder}]");
            }

            private static bool ThrowException<T>(string name, T value, string tail) =>
                throw new ValidationException($"{name}={value} {tail}");
        }
    }

    public class ValidationException : ArgumentException
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}
