using SoftFx.Common.Extensions;
using System.Text;

namespace ImportAccountStateBot.Extensions
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendAccountState(this StringBuilder sb, AccountState state, string stateName)
        {
            sb.AppendLine($"{stateName} state{(state == null ? string.Empty : $" ({state.StateTime.NormalDateForm()})")}:");
            sb.AppendLine($"{(state == null || state.IsEmpty ? "Empty\n" : state.ToString())}");

            return sb;
        }

        public static StringBuilder AppendTimeToNextState(this StringBuilder sb, AccountState state, ITimeProvider timeProvider)
        {
            var time = timeProvider.UtcNow;

            sb.Append($"Current time: {time.NormalDateForm()}");

            if (state != null)
                sb.Append($", next state in {(long)(state.StateTime - time).TotalSeconds} sec");

            return sb.AppendLine();
        }
    }
}
