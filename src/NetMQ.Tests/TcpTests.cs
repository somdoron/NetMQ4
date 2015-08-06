using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NetMQ.Tests
{
    [TestFixture]
    public class TcpTests
    {
        [Test]
        public void Bind()
        {
            using (PairSocket bind = new PairSocket())
            {
                bind.TcpBind("localhost", 45000);
                Thread.Sleep(5000);
            }
        }
    }
}
