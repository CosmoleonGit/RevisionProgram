﻿using RevisionProgram2.dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RevisionProgram2.folderSync
{
    public class ServerSync : OnlineSync
    {
        readonly TcpListener server;
        TcpClient client;

        public ServerSync(int port)
        {
            server = new TcpListener(IPAddress.Any, port);
        }

        public override bool Setup()
        {
            try
            {
                server.Start();
            } catch (SocketException)
            {
                MsgBox.ShowWait("Failed to start server. Is there already a server listening on that port?",
                                "Error",
                                null,
                                MsgBox.MsgIcon.ERROR);

                return false;
            }
            
            WaitingForm.BeginWait("Waiting for client to connect...", ev =>
            {
                while (true)
                {
                    try
                    {
                        var waiting = Task.Run(async delegate { await Task.Delay(timeout); });
                        
                        while (!server.Pending() && !waiting.IsCompleted) { }

                        if (server.Pending())
                        {
                            client = server.AcceptTcpClient();
                            socket = client.Client;
                            socket.SendBufferSize = bufferSize;
                            socket.ReceiveBufferSize = bufferSize;
                            stream = client.GetStream();
                            stream.ReadTimeout = timeout;

                            break;
                        }
                        else
                        {
                            throw new TimeoutException();
                        }
                    }
                    catch (TimeoutException)
                    {
                        if (!ConnectionFailure()) break;
                    } catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }, () =>
            {
                server.Stop();

                return true;
            });

            if (socket != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Finish()
        {
            base.Finish();
            server.Stop();
        }
    }
}
