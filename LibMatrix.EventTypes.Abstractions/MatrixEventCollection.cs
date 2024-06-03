using System.Collections;

namespace LibMatrix.EventTypes.Abstractions;

public interface IMatrixEventCollection<out T> : IEnumerable<IMatrixEvent<T>> where T : BaseMatrixEventContent {
    
}
public class MatrixEventCollection : IMatrixEventCollection<BaseMatrixEventContent>, IList<MatrixEvent<BaseMatrixEventContent>> {
    private IList<MatrixEvent<BaseMatrixEventContent>> _listImplementation;
    IEnumerator<IMatrixEvent<BaseMatrixEventContent>> IEnumerable<IMatrixEvent<BaseMatrixEventContent>>.GetEnumerator() => GetEnumerator();

    public IEnumerator<MatrixEvent<BaseMatrixEventContent>> GetEnumerator() => _listImplementation.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_listImplementation).GetEnumerator();

    public void Add(MatrixEvent<BaseMatrixEventContent> item) => _listImplementation.Add(item);

    public void Clear() => _listImplementation.Clear();

    public bool Contains(MatrixEvent<BaseMatrixEventContent> item) => _listImplementation.Contains(item);

    public void CopyTo(MatrixEvent<BaseMatrixEventContent>[] array, int arrayIndex) => _listImplementation.CopyTo(array, arrayIndex);

    public bool Remove(MatrixEvent<BaseMatrixEventContent> item) => _listImplementation.Remove(item);

    public int Count => _listImplementation.Count;

    public bool IsReadOnly => _listImplementation.IsReadOnly;

    public int IndexOf(MatrixEvent<BaseMatrixEventContent> item) => _listImplementation.IndexOf(item);

    public void Insert(int index, MatrixEvent<BaseMatrixEventContent> item) => _listImplementation.Insert(index, item);

    public void RemoveAt(int index) => _listImplementation.RemoveAt(index);

    public MatrixEvent<BaseMatrixEventContent> this[int index] {
        get => _listImplementation[index];
        set => _listImplementation[index] = value;
    }
}
public class MatrixEventCollection<T> : IMatrixEventCollection<T>, IList<MatrixEvent<T>> where T : BaseMatrixEventContent {
    //TODO: implement
    
    private IList<MatrixEvent<T>> _listImplementation = new List<MatrixEvent<T>>();
    IEnumerator<IMatrixEvent<T>> IEnumerable<IMatrixEvent<T>>.GetEnumerator() => GetEnumerator();

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