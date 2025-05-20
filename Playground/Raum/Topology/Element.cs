namespace Playground.Raum.Topology
{
    internal abstract class Element
    {
        public HashSet<Guid> RefIds { get; } = new HashSet<Guid>();
    }
}
