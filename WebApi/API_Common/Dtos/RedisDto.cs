using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos
{
    public class RedisDto
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string Key { get; set; }
    }
    public class CreateKeyRedisDto
    {
        [Required]
        public string AppCode { get; set; }
        [Required]
        public string Prefix { get; set; }
        [Required]
        public string Key { get; set; }
        public int ExpTime { get; set; }
        [Required]
        public string Value { get; set; }
    }
    public class SetRedisApiPartnerDto
    {
        [Required]
        public string Key { get; set; }
        [Required]
        public string HashField { get; set; }
        [Required]
        public Object Data { get; set; }
    }
}
