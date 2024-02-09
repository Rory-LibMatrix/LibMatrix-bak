namespace LibMatrix.Extensions;

public static class EnumerableExtensions {
    public static void MergeStateEventLists(this IList<StateEvent> oldState, IList<StateEvent> newState) {
        foreach (var stateEvent in newState) {
            var old = oldState.FirstOrDefault(x => x.Type == stateEvent.Type && x.StateKey == stateEvent.StateKey);
            if (old is null) {
                oldState.Add(stateEvent);
                continue;
            }

            oldState.Remove(old);
            oldState.Add(stateEvent);
        }
    }

    public static void MergeStateEventLists(this IList<StateEventResponse> oldState, IList<StateEventResponse> newState) {
        foreach (var stateEvent in newState) {
            var old = oldState.FirstOrDefault(x => x.Type == stateEvent.Type && x.StateKey == stateEvent.StateKey);
            if (old is null) {
                oldState.Add(stateEvent);
                continue;
            }

            oldState.Remove(old);
            oldState.Add(stateEvent);
        }
    }
}