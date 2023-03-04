using HkmpPouch;
using System.Globalization;

namespace ESoulLink.Events
{

    public class JoinPoolEvent : PipeEvent
    {
        internal static char[] separator = new char[] { '|' };

        internal static string Name = "JoinPool";

        public string BossName = "";

        public int WithHealth = 0;
        public override string GetName()
        {
            return JoinPoolEvent.Name;
        }

        public override string ToString()
        {
            return $"{BossName}{separator[0]}{WithHealth.ToString(CultureInfo.InvariantCulture)}";

        }

    }
    public class JoinPoolEventFactory : IEventFactory 
    {
        public static JoinPoolEventFactory Instance = new JoinPoolEventFactory();
        public string GetName()
        {
            return JoinPoolEvent.Name;
        }

        public PipeEvent FromSerializedString(string serializedData)
        {
            var Event = new JoinPoolEvent();
            var split = serializedData.Split(JoinPoolEvent.separator);
            Event.BossName = split[0];
            Event.WithHealth = int.Parse(split[1], CultureInfo.InvariantCulture);
            return Event;
        }
    }
}
