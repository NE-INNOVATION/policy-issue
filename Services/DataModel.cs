using System.Collections.Generic;
using System.Linq;

namespace policy_issue.Services{
    public class DataModel
    {
        public static List<string> Messages;

        public string GetMessage()
        {
            if(Messages != null && Messages.Count > 0)
            {
                var data =new [] { Messages[0] };
                Messages.RemoveAt(0);
                return data[0];
            }
            return "";
        }

        public void SetMessage(string data)
        {
            if(Messages == null ) Messages = new List<string>();
            Messages.Add(data);
        }
        
    }
}