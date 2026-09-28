/** UI copy per language. `pt` is the source of truth; `en` must match its shape. */
const pt = {
  common: {
    signIn: "Entrar",
    back: "Voltar",
    getStarted: "Começar agora",
    theme: "Alternar tema claro/escuro",
    language: "Idioma",
    genericError: "Algo correu mal. Tente novamente.",
  },
  landing: {
    navLabel: "Principal",
    nav: {
      features: "Funcionalidades",
      how: "Como funciona",
      partners: "Parceiros",
      about: "Sobre",
    },
    eyebrow: "Software para associações",
    title: "Gerir a sua associação nunca foi tão simples",
    lead: "Reservas de espaços, empréstimos de equipamento, voluntários, workshops e despesas num só lugar.",
    seeHow: "Ver como funciona",
    heroAlt: "Participantes num workshop comunitário",
    partnersEyebrow: "Organizações parceiras",
    partnersLabel: "Parceiros",
    featuresEyebrow: "O que inclui",
    featuresTitle: "Funcionalidades do Nexo",
    features: [
      {
        title: "Reservas de espaços",
        text: "Marque salas de ensaio, salas de workshops e campos desportivos municipais sem conflitos de horário.",
      },
      {
        title: "Gestão de equipamento",
        text: "Controle saídas, devoluções e o inventário de projetores, mesas e outro material.",
      },
      {
        title: "Coordenação de voluntários",
        text: "Gira escalas de serviço, registe disponibilidades e comunique o que o centro precisa.",
      },
      {
        title: "Manutenção",
        text: "Sinalize avarias, abra pedidos de reparação e acompanhe a manutenção preventiva das instalações.",
      },
      {
        title: "Gestão de despesas",
        text: "Registe as despesas de manutenção e das atividades e mantenha as contas à vista de todos.",
      },
      {
        title: "Histórico de decisões",
        text: "Guarde as atas das reuniões de direção e as decisões da assembleia.",
      },
    ],
    stepsEyebrow: "Primeiros passos",
    stepsTitle: "Como funciona o Nexo",
    steps: [
      {
        alt: "Pessoa a preencher o registo da associação num portátil",
        title: "Registe a sua associação",
        text: "Crie a ficha da associação, convide a comissão administrativa e defina as permissões de cada pessoa.",
      },
      {
        alt: "Pavilhão desportivo com bancadas",
        title: "Configure espaços e recursos",
        text: "Adicione as salas do centro e o inventário de equipamento que pode ser emprestado, com fotografias.",
      },
      {
        alt: "Voluntários a atender numa mesa de inscrições",
        title: "Comece a gerir",
        text: "Abra as reservas, registe as decisões, acompanhe as despesas e convoque os voluntários.",
      },
    ],
    ctaTitle: "Pronto para simplificar a gestão?",
    ctaText:
      "Deixe o papel e as folhas de cálculo. Reservas, equipamento e despesas ficam num só sítio.",
    createAccount: "Criar conta",
    contactUs: "Falar connosco",
    footerAbout:
      "Software de gestão para associações locais e comissões comunitárias.",
    footerColumns: [
      {
        title: "Produto",
        links: ["Funcionalidades", "Reservas", "Equipamentos", "Voluntários"],
      },
      { title: "Empresa", links: ["Sobre nós", "Notícias", "Contacto"] },
      {
        title: "Legal",
        links: ["Termos de uso", "Privacidade", "RGPD", "Licenciamento"],
      },
    ],
    copyright: "© 2026 Nexo. Todos os direitos reservados.",
  },
  auth: {
    signInTitle: "Iniciar sessão",
    registered:
      "Organização registada. Inicie sessão com a conta de administrador.",
    googleFailed: "O início de sessão com Google falhou.",
    newToNexo: "A sua associação ainda não usa o Nexo?",
    createAnAccount: "Registar a organização",
    email: "Email",
    password: "Palavra-passe",
    signInButton: "Iniciar sessão",
    google: "Continuar com Google",
    enterCredentials: "Indique o email e a palavra-passe.",
    badCredentials: "O email ou a palavra-passe não estão corretos.",
    registerTitle: "Registe a sua organização",
    organizationName: "Nome da organização",
    firstName: "Nome próprio",
    lastName: "Apelido",
    createButton: "Registar organização",
    fillEvery: "Preencha todos os campos.",
    emailTaken: "Este email já está registado.",
    haveAccount: "Já é membro de uma organização?",
    signInLink: "Iniciar sessão",
    signInSubtitle: "Entre na conta de membro da sua organização.",
    registerSubtitle:
      "Crie a organização e a conta de administrador. Os restantes membros são convidados por si.",
    activateTitle: "Crie a sua conta de membro",
    activateSubtitle:
      "Introduza o código do convite recebido por email para continuar.",
    invitationCode: "Código do convite",
    activateButton: "Criar conta",
    activated: "Conta ativada. Inicie sessão para continuar.",
    invitationInvalid: "O convite não é válido. Peça um novo ao administrador.",
    activateConflict:
      "A conta já está ativa ou a organização atingiu o limite de membros.",
    resendInvite: "Não recebeu o código, ou já expirou?",
    resendButton: "Reenviar convite",
    resendSent:
      "Se esse email tiver um convite pendente, foi enviado um novo código.",
    codeVerified: "Código confirmado.",
    showPassword: "Mostrar palavra-passe",
    hidePassword: "Ocultar palavra-passe",
    or: "ou",
    panelTitle: "Tudo o que a sua associação precisa, num só lugar.",
    panelPoints: [
      "Reservas de espaços sem conflitos de horário",
      "Equipamento e voluntários sempre organizados",
      "Despesas e decisões à vista de todos",
    ],
    panelNote: "Software para associações e comissões comunitárias.",
  },
};

const en: typeof pt = {
  common: {
    signIn: "Sign in",
    back: "Back",
    getStarted: "Get started",
    theme: "Toggle light/dark theme",
    language: "Language",
    genericError: "Something went wrong. Try again.",
  },
  landing: {
    navLabel: "Main",
    nav: {
      features: "Features",
      how: "How it works",
      partners: "Partners",
      about: "About",
    },
    eyebrow: "Software for associations",
    title: "Running your association has never been this simple",
    lead: "Space bookings, equipment loans, volunteers, workshops and expenses in one place.",
    seeHow: "See how it works",
    heroAlt: "Participants at a community workshop",
    partnersEyebrow: "Partner organizations",
    partnersLabel: "Partners",
    featuresEyebrow: "What is included",
    featuresTitle: "Nexo features",
    features: [
      {
        title: "Space bookings",
        text: "Book rehearsal rooms, workshop rooms and municipal sports fields without scheduling conflicts.",
      },
      {
        title: "Equipment management",
        text: "Track check-outs, returns and the inventory of projectors, tables and other gear.",
      },
      {
        title: "Volunteer coordination",
        text: "Manage duty rosters, record availability and share what the centre needs.",
      },
      {
        title: "Maintenance",
        text: "Report faults, open repair requests and follow preventive maintenance of the premises.",
      },
      {
        title: "Expense management",
        text: "Record maintenance and activity expenses and keep the accounts visible to everyone.",
      },
      {
        title: "Decision history",
        text: "Keep the minutes of board meetings and the decisions of the general assembly.",
      },
    ],
    stepsEyebrow: "Getting started",
    stepsTitle: "How Nexo works",
    steps: [
      {
        alt: "Person filling in the association registration on a laptop",
        title: "Register your association",
        text: "Create the association profile, invite the administrative committee and set each person's permissions.",
      },
      {
        alt: "Sports hall with bleachers",
        title: "Set up spaces and resources",
        text: "Add the rooms of the centre and the equipment inventory that can be lent, with photos.",
      },
      {
        alt: "Volunteers staffing a sign-up table",
        title: "Start managing",
        text: "Open bookings, record decisions, track expenses and call on volunteers.",
      },
    ],
    ctaTitle: "Ready to simplify your management?",
    ctaText:
      "Leave the paper and spreadsheets behind. Bookings, equipment and expenses live in one place.",
    createAccount: "Create account",
    contactUs: "Contact us",
    footerAbout:
      "Management software for local associations and community committees.",
    footerColumns: [
      {
        title: "Product",
        links: ["Features", "Bookings", "Equipment", "Volunteers"],
      },
      { title: "Company", links: ["About us", "News", "Contact"] },
      {
        title: "Legal",
        links: ["Terms of use", "Privacy", "GDPR", "Licensing"],
      },
    ],
    copyright: "© 2026 Nexo. All rights reserved.",
  },
  auth: {
    signInTitle: "Sign in",
    registered: "Organization registered. Sign in with the admin account.",
    googleFailed: "Google sign-in failed.",
    newToNexo: "Is your association not on Nexo yet?",
    createAnAccount: "Register your organization",
    email: "Email",
    password: "Password",
    signInButton: "Sign in",
    google: "Continue with Google",
    enterCredentials: "Enter your email and password.",
    badCredentials: "The email or password is not correct.",
    registerTitle: "Register your organization",
    organizationName: "Organization name",
    firstName: "First name",
    lastName: "Last name",
    createButton: "Register organization",
    fillEvery: "Fill in every field.",
    emailTaken: "This email is already registered.",
    haveAccount: "Already a member of an organization?",
    signInLink: "Sign in",
    signInSubtitle: "Sign in to your member account.",
    registerSubtitle:
      "Create the organization and its admin account. You invite the other members.",
    activateTitle: "Create your member account",
    activateSubtitle: "Enter the invitation code from your email to continue.",
    invitationCode: "Invitation code",
    activateButton: "Create account",
    activated: "Account activated. Sign in to continue.",
    invitationInvalid:
      "This invitation is not valid. Ask your admin for a new one.",
    activateConflict:
      "The account is already active or the organization reached its member limit.",
    resendInvite: "Didn't get the code, or has it expired?",
    resendButton: "Resend invitation",
    resendSent:
      "If that email has a pending invitation, a new code was just sent.",
    codeVerified: "Code confirmed.",
    showPassword: "Show password",
    hidePassword: "Hide password",
    or: "or",
    panelTitle: "Everything your association needs, in one place.",
    panelPoints: [
      "Space bookings without scheduling conflicts",
      "Equipment and volunteers always organized",
      "Expenses and decisions visible to everyone",
    ],
    panelNote: "Software for associations and community committees.",
  },
};

export const messages = { pt, en };
export type Lang = keyof typeof messages;
