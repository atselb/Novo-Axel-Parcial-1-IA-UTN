
public class StateMachine
{
    private IState currentState;

    public IState CurrentState => currentState;

    public void ChangeState(IState newState)
    {
        if (newState == null) return;

        currentState?.Exit();

        currentState = newState;

        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}
