using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NetMQ.Tests
{
    [TestFixture]
    public class InProcTests
    {
        [Test]
        public void BindAndConnect()
        {
            using (PairSocket bind = new PairSocket())
            {
                bind.Bind("inproc://test");
                Assert.That(!bind.HasOut);

                using (PairSocket connect = new PairSocket())
                {
                    connect.Connect("inproc://test");
                    
                    Assert.That(connect.HasOut);
                    Assert.That(bind.HasOut);
                    Assert.That(!bind.HasIn);
                   
                    Assert.That(connect.TrySendFrame("Hello"));                    
                    Assert.That(bind.HasIn);

                    string message;

                    Assert.That(bind.TryReceiveFrameString(out message));
                    Assert.That(message == "Hello");                    
                }
            }
        }

        [Test]
        public void ConnectBeforeBind()
        {
            using (PairSocket bind = new PairSocket())
            {                
                using (PairSocket connect = new PairSocket())
                {
                    connect.Connect("inproc://test");                                        
                    bind.Bind("inproc://test");

                    Assert.That(connect.HasOut);
                    Assert.That(bind.HasOut);
                    Assert.That(!bind.HasIn);

                    Assert.That(connect.TrySendFrame("Hello"));
                    Assert.That(bind.HasIn);

                    string message;

                    Assert.That(bind.TryReceiveFrameString(out message));
                    Assert.That(message == "Hello");           
                }
            }
        }
    }
}
