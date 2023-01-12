namespace CoFiSo;

public delegate void VectorChangedEventHandler<T>(IObservableVector<T> sender, IVectorChangedEventArgs @event);
public delegate void VectorChangedEventHandler(object sender, IVectorChangedEventArgs @event);
    