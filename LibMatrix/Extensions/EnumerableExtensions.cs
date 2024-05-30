namespace LibMatrix.Extensions;

public static class EnumerableExtensions {
    public static void MergeStateEventLists(this IList<LegacyMatrixEvent> oldState, IList<LegacyMatrixEvent> newState) {
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

    public static void MergeStateEventLists(this IList<LegacyMatrixEventResponse> oldState, IList<LegacyMatrixEventResponse> newState) {
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