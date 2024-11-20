using HkmpPouch;
using System;
using System.Configuration;
using System.Globalization;

namespace FightTogether.Events
{
    public enum HealthOperation
    {
        Init,
        Update
    }
    // Used so clients can tell the server the current health of the enemy they are fighting
    // Used so the server can push updates to the clients
    public class HealthEvent(HealthOperation operation, string entityName, int health) : PipeEvent
    {
        public static string Name = "UpdateHealth";

        public string entityName = entityName;

        public int health = health;

        public HealthOperation operation = operation;
        public override string GetName()
        {
            return Name;
        }

        public override string ToString()
        {
            return $"{operation}{Constants.DataSeparator}{entityName}{Constants.DataSeparator}{health.ToString(CultureInfo.InvariantCulture)}";

        }
    }
    public class UpdateHealthEventFactory : IEventFactory
    {
        public static UpdateHealthEventFactory Instance = new();

        public PipeEvent FromSerializedString(string serializedData)
        {
            var tokens = serializedData.Split(Constants.DataSeparator);
            return new HealthEvent((HealthOperation)Enum.Parse(typeof(HealthOperation), tokens[0]), tokens[1], int.Parse(tokens[2]));
        }

        public string GetName()
        {
            return HealthEvent.Name;
        }
    }
}
