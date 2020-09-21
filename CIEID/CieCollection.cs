using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CIEID
{
    class CieCollection
    {
        private Dictionary<string, List<string>> mydictionary;


        public CieCollection()
        {
            mydictionary = new Dictionary<string, List<string>>();
        }

        public CieCollection(string collectionString)
        {
            if(collectionString.Equals(""))
            {
                mydictionary = new Dictionary<string, List<string>>();
            }
            else
            {
                mydictionary = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(collectionString);
            }
        }

        public Dictionary<string, List<string>> MyDictionary
        {
            get { return mydictionary; }
            set { mydictionary = value; }
        }

        public void addCie(String pan, String owner, String serialNumber)
        {
            if (mydictionary != null)
                mydictionary.Add(pan, new List<string> { owner, serialNumber });
        }

        public void removeCie(String pan)
        {
            if (mydictionary != null)
                mydictionary.Remove(pan);
        }

        public void removeAllCie()
        {
            if (mydictionary != null)
                mydictionary.Clear();
        }
    }
}
