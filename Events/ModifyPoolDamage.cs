using HkmpPouch;
using System.Globalization;

namespace FightTogether.Events
{
    public class ModifyPoolHealthEvent : PipeEvent
    {

        internal static char[] separator = new char[] { '|' };


        public static string Name = "ModifyPool";

        public string BossName = "";

        public int ReduceHealthBy = 0;
        public override string GetName()
        {
            return ModifyPoolHealthEvent.Name;
        }

        public override string ToString()
        {
            return $"{BossName}{separator[0]}{ReduceHealthBy.ToString(CultureInfo.InvariantCulture)}";

        }
    }
    public class ModifyPoolHealthEventFactory : IEventFactory
    {
        public static ModifyPoolHealthEventFactory Instance = new ModifyPoolHealthEventFactory();

        public PipeEvent FromSerializedString(string serializedData)
        {
            var Event = new ModifyPoolHealthEvent();
            var split = serializedData.Split(ModifyPoolHealthEvent.separator);
            Event.BossName = split[0];
            Event.ReduceHealthBy = int.Parse(split[1], CultureInfo.InvariantCulture);
            return Event;
        }

        public string GetName()
        {
            return ModifyPoolHealthEvent.Name;
        }
    }
}
