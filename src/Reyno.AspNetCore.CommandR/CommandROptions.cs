namespace Reyno.AspNetCore.CommandR {

    public class CommandROptions {

        public string Path { get; set; } = "_commandr";

        public string RequestNamespace { get; set; }

        public bool RequireAuthorization { get; set; } = false;

        public bool UseAuthorization { get; set; } = true;

        public bool UseValidation { get; set; } = true;

        public bool AllowNoContext { get; set; } = true;
    }
}