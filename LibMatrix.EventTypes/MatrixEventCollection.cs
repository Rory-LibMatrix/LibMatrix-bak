using System.Collections;

namespace LibMatrix.EventTypes;

public interface IMatrixEventCollection<out T> : IEnumerable<IMatrixEvent<T>> where T : MatrixEventContent {
    
}
public class MatrixEventCollection : IMatrixEventCollection<MatrixEventContent>, IList<MatrixEvent<MatrixEventContent> {
    private IList<MatrixEvent<MatrixEventContent>> _listImplementation;
    public IEnumerator<MatrixEvent<MatrixEventContent>> GetEnumerator() => _listImplementation.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_listImplementation).GetEnumerator();

    public void Add(MatrixEvent<MatrixEventContent> item) => _listImplementation.Add(item);

    public void Clear() => _listImplementation.Clear();

    public bool Contains(MatrixEvent<MatrixEventContent> item) => _listImplementation.Contains(item);

    public void CopyTo(MatrixEvent<MatrixEventContent>[] array, int arrayIndex) => _listImplementation.CopyTo(array, arrayIndex);

    public bool Remove(MatrixEvent<MatrixEventContent> item) => _listImplementation.Remove(item);

    public int Count => _listImplementation.Count;

    public bool IsReadOnly => _listImplementation.IsReadOnly;

    public int IndexOf(MatrixEvent<MatrixEventContent> item) => _listImplementation.IndexOf(item);

    public void Insert(int index, MatrixEvent<MatrixEventContent> item) => _listImplementation.Insert(index, item);

    public void RemoveAt(int index) => _listImplementation.RemoveAt(index);

    public MatrixEvent<MatrixEventContent> this[int index] {
        get => _listImplementation[index];
        set => _listImplementation[index] = value;
    }
}
public class MatrixEventCollection<T> : IMatrixEventCollection<T>, IList<MatrixEvent<T>> where T : MatrixEventContent {
    //TODO: implement
    
    private IList<MatrixEvent<T>> _listImplementation = new List<MatrixEvent<T>>();
    public IEnumerator<MatrixEvent<T>> GetEnumerator() => _listImplementation.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_listImplementation).GetEnumerator();

    public void Add(MatrixEvent<T> item) => _listImplementation.Add(item);

    public void Clear() => _listImplementation.Clear();

    public bool Contains(MatrixEvent<T> item) => _listImplementation.Contains(item);

    public void CopyTo(MatrixEvent<T>[] array, int arrayIndex) => _listImplementation.CopyTo(array, arrayIndex);

    public bool Remove(MatrixEvent<T> item) => _listImplementation.Remove(item);

    public int Count => _listImplementation.Count;

    public bool IsReadOnly => _listImplementation.IsReadOnly;

    public int IndexOf(MatrixEvent<T> item) => _listImplementation.IndexOf(item);

    public void Insert(int index, MatrixEvent<T> item) => _listImplementation.Insert(index, item);

    public void RemoveAt(int index) => _listImplementation.RemoveAt(index);

    public MatrixEvent<T> this[int index] {
        get => _listImplementation[index];
        set => _listImplementation[index] = value;
    }
}