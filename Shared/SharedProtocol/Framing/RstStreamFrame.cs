using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.Framing
{
    public class RstStreamFrame : Frame
    {
        // The number of bytes in the frame.
        private const int InitialFrameSize = 12;

        // Incoming
        public RstStreamFrame(Frame preamble)
            : base(preamble)
        {
        }

        // Outgoing
        public RstStreamFrame(int id, ResetStatusCode statusCode)
            : base(new byte[InitialFrameSize])
        {
            StreamId = id;//32 bit
            FrameType = FrameType.RstStream;//8bit
            FrameLength = InitialFrameSize - Constants.FramePreambleSize; // 16bit
            StatusCode = statusCode;//32bit
        }

        // 32 bits
        public ResetStatusCode StatusCode
        {
            get
            {
                return (ResetStatusCode)FrameHelpers.Get32BitsAt(Buffer, 8);
            }
            set
            {
                FrameHelpers.Set32BitsAt(Buffer, 8, (int)value);
            }
        }
    }
}
