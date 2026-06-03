using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Response
{
    public class ResponseModel<T, EStatus>
    {
        /// <summary>
        /// Gets or sets Status response.
        /// </summary>
        public EStatus Status { get; set; }

        /// <summary>
        /// Gets or sets Message info.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets Oject data response.
        /// </summary>
        public T Data { get; set; }
        /// <summary>
        /// Ojbect error from vinid.
        /// </summary>
       // public ErrorModel ErrorModel { get; set; }
    }
}
