
public abstract class HunterState
{
    protected readonly HunterAgent hunter;

    protected HunterState(HunterAgent hunter)
    {
        this.hunter = hunter;
    }

    public virtual void Enter() { }

    public virtual void Update() { }

    public virtual void Exit() { }
}
