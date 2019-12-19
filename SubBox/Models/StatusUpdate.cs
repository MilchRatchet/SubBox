namespace SubBox.Models
{
    public class StatusUpdate
    {
        public string Kind { get; set; }

        public string Key { get; set; }

        public bool Value { get; set; }

        /*
         * Implemented variants of kind:
         * - noStatus { key = '', value = (false: default)}
         * - downloadResult { key = 'id of video', value = (false: failed, true: finished)}
         * 
         * 
         */
    }
}
