using System;

namespace SharedProtocol
{
    public class FlowControlOptions
    {
        public Int32 InitialWindowSize { get; set; }

        public Int32 OptionsValue { get; set; }

        public FlowControlOptions()
            :this(Constants.DefaultFlowControlCredit, Constants.InitialFlowControlOptionsValue)
        {
        }

        public FlowControlOptions(Int32 initialWindowSize, Int32 optionsValue)
        {
            this.InitialWindowSize = initialWindowSize;
            this.OptionsValue = optionsValue;
        }

        public bool IsSessionFlowControlEnabled
        {
            get { return OptionsValue % 2 == 0; }
        }

        public bool IsNewStreamsFlowControlled
        {
            get
            {
                return OptionsValue >> 1 % 2 == 0;
            }
        }
    }
}
