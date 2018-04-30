using System;
using System.Net;
public static class Class1
{
    int port { get; set; }
    int bufferSize { get; set; }
    Socket serverSocket { get; set; }
    List<Socket> listSockets = new List<Socket>();
    byte[] buffer { get; set; }
    public Class1()
	{

	}
}
