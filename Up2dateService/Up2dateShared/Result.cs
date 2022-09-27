using System;
using System.Runtime.Serialization;
using System.Text;

namespace Up2dateShared
{
    [DataContract(Namespace = "http://RTSoft.Ritms.Up2date.win")]
    public struct Result
    {
        public static Result Successful()
        {
            return new Result { Success = true, ErrorMessage = null };
        }

        public static Result Failed(string errorMessage = null)
        {
            return new Result { Success = false, ErrorMessage = errorMessage };
        }

        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
    }

    [DataContract(Namespace = "http://RTSoft.Ritms.Up2date.win")]
    public struct Result<T>
    {
        public static Result<T> Successful(T value)
        {
            return new Result<T> { Value = value, Success = true, ErrorMessage = null };
        }

        public static Result<T> Failed(string errorMessage = null)
        {
            return new Result<T> { Success = false, ErrorMessage = errorMessage };
        }

        public static Result<T> Failed(Exception exception)
        {
            var msgBuilder = new StringBuilder();
            for (var e = exception; e != null; e = e.InnerException)
            {
                if (e is AggregateException) continue;
                msgBuilder.AppendLine(e.Message);
            }
            return new Result<T> { Success = false, ErrorMessage = msgBuilder.ToString() };
        }

        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public T Value { get; set; }
    }
}
