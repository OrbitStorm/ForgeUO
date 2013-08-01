using Server;

namespace CustomsFramework.Systems.ShardControl
{
    public abstract class BaseSettings
    {
        public abstract override string ToString();

        public abstract void Serialize(GenericWriter writer);

        public abstract void Deserialize(GenericReader reader);
    }
}