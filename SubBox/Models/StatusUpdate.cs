namespace SubBox.Models
{
    public class StatusUpdate
    {
        public string Kind { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        /*
         * Implemented variants of kind:
         * - noStatus { key = '', value = ''}
         * - downloadResult { key = 'id of video', value = ('false': failed, 'true': finished)}
         * - downloadProgress { key = 'id of video', value = ('XX': two digit percent representation of download progress)}
         * - channelResult { key = 'requestString', value = ('false': failed, 'true': finished)}
         * 
         */
    }
}
