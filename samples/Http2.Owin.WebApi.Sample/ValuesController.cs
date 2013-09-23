using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Owin.Test.WebApiTest
{
    public class ValuesController : ApiController
    {
        private static Dictionary<int, string> _store = null;

        public ValuesController()
        {
            if (_store == null)
            {
                _store = new Dictionary<int, string>();

                _store.Add(0, "value#0");
                _store.Add(1, "value#1");
            }
        }

        // GET api/values 
        public IEnumerable<string> Get()
        {
            return _store.Values.ToArray();
        }

        // GET api/values/5 
        public string Get(int id)
        {
            if (_store.ContainsKey(id))
            {
                return _store[id];
            }

            return null;
        }

        // POST api/values 
        public void Post([FromBody]string value)
        {
            int max;
            if (_store.Keys.Count > 0)
            {
                max = _store.Keys.Max();
            }
            else
            {
                max = 0;
            }

            _store[max + 1] = value;
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value)
        {
            if (_store.ContainsKey(id))
            {
                _store[id] = value;
            }
            else
            {
                _store.Add(id, value);
            }
        }

        // DELETE api/values/5 
        public void Delete(int id)
        {
            if (_store.ContainsKey(id))
            {
                _store.Remove(id);
            }
        }
    }

}