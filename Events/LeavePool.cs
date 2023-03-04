using HkmpPouch;

namespace ESoulLink.Events
{
    public class LeavePoolEvent : PipeEvent
    {
        internal static char[] separator = new char[] { '|' };

        internal static string Name = "LeavePool";

        public string BossName = "";
        public override string GetName()
        {
            return LeavePoolEvent.Name;
        }

        public override string ToString()
        {
            return BossName;
        }

    }
    public class LeavePoolEventFactory : IEventFactory
    {
        public static LeavePoolEventFactory Instance = new LeavePoolEventFactory();
        public string GetName()
        {
            return LeavePoolEvent.Name;
        }

        public PipeEvent FromSerializedString(string serializedData)
        {
            var Event = new LeavePoolEvent();
            Event.BossName = serializedData;
            return Event;
        }
    }
}
