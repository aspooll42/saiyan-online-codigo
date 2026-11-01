using System;

namespace Intersect.Client.Interface.Data;

public delegate void DataProviderEventHandler<in TDataProvider, in TEventArgs>(TDataProvider sender, TEventArgs args) where TDataProvider : IDataProvider where TEventArgs : EventArgs;
public delegate void DataProviderEventHandler<in TEventArgs>(IDataProvider sender, TEventArgs args) where TEventArgs : EventArgs;
