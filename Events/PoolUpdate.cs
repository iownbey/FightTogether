using HkmpPouch;
using System.Globalization;

namespace ESoulLink.Events
{
    public class PoolUpdateEvent : PipeEvent
    {
        internal static char[] separator = new char[] { '|' };

        internal static string Name = "UpdatePool";

        public string BossName = "";

        public int CurrentHealth = 0;

        public override string GetName()
        {
            return PoolUpdateEvent.Name;
        }

        public override string ToString()
        {
            return $"{BossName}{separator[0]}{CurrentHealth.ToString(CultureInfo.InvariantCulture)}";
        }
    }

    public class PoolUpdateEventFactory : IEventFactory
    {
        public static PoolUpdateEventFactory Instance = new PoolUpdateEventFactory();
        public PipeEvent FromSerializedString(string serializedData)
        {
            var Event = new PoolUpdateEvent();
            var split = serializedData.Split(PoolUpdateEvent.separator);
            Event.BossName = split[0];
            Event.CurrentHealth = int.Parse(split[1], CultureInfo.InvariantCulture);
            return Event;
        }

        public string GetName()
        {
            return PoolUpdateEvent.Name;
        }
    }
}
