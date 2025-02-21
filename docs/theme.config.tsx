import {
  defineTheme,
  directory,
  group,
  link,
  site,
  siteTemplate,
  social,
  type SiteComponent,
} from '@neato/guider/theme';
import { Logo } from 'components/logo';
import { ghPrefix } from './components/gh-prefix';

const starLinks = [
  link('GitHub', 'https://github.com/eventstorage/eventstorage', {
    style: 'star',
    newTab: true,
    icon: 'akar-icons:github-fill',
  }),
  link('Discord', 'https://discord.gg/cGd5pfKxWyK', {
    style: 'star',
    newTab: true,
    icon: 'fa6-brands:discord',
  }),
  link(
    'Get involved',
    'https://github.com/eventstorage/eventstorage/issues',
    {
      style: 'star',
      newTab: true,
      icon: 'streamline:chat-bubble-typing-oval-solid',
    },
  ),
];

const v0_0_0_beta_16 = (url: string) => `/v0.0.0-beta.16${url}`;
const v0_0_0_beta_15 = (url: string) => `/v0.0.0-beta.15${url}`;

const template = siteTemplate({
  github: 'eventstorage/eventstorage',
  dropdown: [
    link('v0.0.0-beta.16', v0_0_0_beta_16("")),
    link('v0.0.0-beta.15', v0_0_0_beta_15("")),
  ],
  navigation: [],
  settings: {
    colors: {
      primary: '#A476D9',
      primaryDarker: '#6C3DD0',
      primaryLighter: '#D0BAFF',
      backgroundLightest: "#282438",
      backgroundLighter: "#1A1726"
    },
    backgroundPattern: 'flare',
    logo: () => <Logo />,
  },
  // settings: {
  //   logo: () => <Logo />,
  //   backgroundPattern: 'flare',
  //   colors: {
  //     "primary": "#A476D9",
  //     "primaryLighter": "#C4ADDE",
  //     "primaryDarker": "#6E23C3",
  //     "background": "#0C0B13",
  //     "backgroundLighter": "#1A1726",
  //     "backgroundLightest": "#282438",
  //     "backgroundDarker": "#000000",
  //     "line": "#37334C",
  //     "text": "#8C899A",
  //     "textLighter": "#A6A4AE",
  //     "textHighlight": "#FFF"
  //   },
  // },
  contentFooter: {
    editRepositoryBase:
      'https://github.com/eventstorage/eventstorage/tree/main/docs',
    socials: [
      social.discord('https://discord.gg/fcGd5pKxWyK'),
      social.github('https://github.com/eventstorage/eventstorage'),
    ],
  },
  meta: {
    titleTemplate: '%s - eventstorage',
    additionalLinkTags: [
      {
        rel: 'icon',
        href: `${ghPrefix()}/2.png`,
      },
    ],
  },
});

export default defineTheme([
  site('main', {
    extends: [template],
    directories: [
      directory('main', {
        sidebar: [],
      }),
    ],
  }),
  site("v0.0.0-beta.16", {
    extends: [template],
    contentFooter: {
      text: 'Copyright © 2025',
    },
    tabs: [
      link('Getting started', v0_0_0_beta_16("/getting-started")),
      link('Guides', v0_0_0_beta_16("/guides")),
      link('Learning', v0_0_0_beta_16("/learn")),
    ],
    directories: [
      directory('getting-started', {
        sidebar: [
          ...starLinks,
          group('Getting started', [
            link('Introduction', v0_0_0_beta_16('/getting-started/getting-started/introduction'), {
              icon: 'icon-park-solid:hi'
            }),
            link('Installation', v0_0_0_beta_16('/getting-started/getting-started/installation'), {
              icon: 'fa6-solid:download'
            }),
            link('Development', v0_0_0_beta_16('/getting-started/getting-started/development'), {
              icon: 'fa6-solid:code'
            }),
          ]),
          group('Configuration', [
            link('Event storage', v0_0_0_beta_16('/getting-started/config/eventstorage'), {
              icon: 'fa6-solid:database'
            }),
            link('Projections', v0_0_0_beta_16('/getting-started/config/projections'), {
              icon: 'icon-park-solid:data'
            }),
            link('Cheers!!', v0_0_0_beta_16('/getting-started/config/cheers'), {
              icon: 'icon-park-solid:success'
            }),
          ]),
          group('Advanced', [
            link('Upcoming', v0_0_0_beta_16('/getting-started/advanced/not-yet')),
          ]),
        ],
      }),
      directory('guides', {
        sidebar: [...starLinks],
      }),
      directory('learn', {
        sidebar: [...starLinks],
      }),
    ],
  }),
  site("v0.0.0-beta.15", {
    extends: [template],
    contentFooter: {
      text: 'Copyright © 2025',
    },
    tabs: [
      link('Getting started', v0_0_0_beta_15("/getting-started")),
      link('Guides', v0_0_0_beta_15("/guides")),
      link('Learning', v0_0_0_beta_15("/learn")),
    ],
    directories: [
      directory('getting-started', {
        sidebar: [
          ...starLinks,
          group('Getting started', [
            link('Installation', v0_0_0_beta_15('/getting-started/getting-started/installation'), {
              icon: 'fa6-solid:download'
            }),
            link('Development', v0_0_0_beta_15('/getting-started/getting-started/development'), {
              icon: 'fa6-solid:code'
            }),
          ]),
          group('Configuration', [
            link('Event storage', v0_0_0_beta_15('/getting-started/config/eventstorage'), {
              icon: 'fa6-solid:database'
            }),
            link('Projections', v0_0_0_beta_15('/getting-started/config/projections'), {
              icon: 'icon-park-solid:data'
            }),
            link('Cheers!!', v0_0_0_beta_15('/getting-started/config/cheers'), {
              icon: 'icon-park-solid:success'
            }),
          ]),
          group('Advanced', [
            link('Upcoming', v0_0_0_beta_15('/getting-started/advanced/not-yet')),
          ]),
        ],
      }),
      directory('guides', {
        sidebar: [...starLinks],
      }),
      directory('learn', {
        sidebar: [...starLinks],
      }),
    ],
  }),
]) satisfies SiteComponent[];
