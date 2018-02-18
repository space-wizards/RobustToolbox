using SS14.Shared.GameObjects;

namespace SS14.Shared.Input
{
    class UserCmd : EntitySystemMessage
    {
        public uint Tick { get; }

        public UserCmd(uint tick)
        {
            Tick = tick;
        }
    }
}
