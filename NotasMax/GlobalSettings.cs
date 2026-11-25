namespace NotasMax
{
    public class GlobalSettings // Classe para armazenar configurações globais da aplicação, como endpoints de API
    {
        // Define o endpoint padrão para a API, que pode ser usado em toda a aplicação para fazer requisições     
#if ANDROID
        public const string DefaultEndpoint = "http://10.0.2.2:5000/NotasMax";
#else
        public const string DefaultEndpoint = "https://nm-aca-check-app.wittyisland-fa59bd74.chilecentral.azurecontainerapps.io/NotasMax";
#endif

        // Define o endpoint específico para autenticação de usuários, que é construído a partir do endpoint padrão
        public string UsuarioLoginAuthEndpoint { get; set; }
        public string UsuarioEndPonint { get; set; }
        public string MateriaEndpoint { get; set; }
        public string SimuladoEndpoint { get; set; }
        public string ForgotPasswordEndpoint { get; set; }
        public string ResetPasswordEndpoint { get; set; }


        public static GlobalSettings Instance { get; } = new GlobalSettings();

        // Constutor da classe, onde o endpoint de autenticação é inicializado usando o endpoint padrão
        public GlobalSettings()
        {
            UsuarioLoginAuthEndpoint = $"{DefaultEndpoint}/Auth/login";
            UsuarioEndPonint = $"{DefaultEndpoint}/Usuarios";
            MateriaEndpoint = $"{DefaultEndpoint}/Materias";
            SimuladoEndpoint = $"{DefaultEndpoint}/Simulados";
            ForgotPasswordEndpoint = $"{DefaultEndpoint}/Auth/forgot-mobile";
            ResetPasswordEndpoint = $"{DefaultEndpoint}/auth/reset";
        }
    }
}
