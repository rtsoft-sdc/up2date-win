using System.Runtime.Serialization;

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

        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public T Value { get; set; }
    }
}
