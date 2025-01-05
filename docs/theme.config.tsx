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

const template = siteTemplate({
  github: 'eventstorage/eventstorage',
  dropdown: [
    link('v0.0.0-beta.13', '/v0.0.0-beta.13'),
    link('v0.0.0-beta.12', '/v0.0.0-beta.12'),
  ],
  navigation: [],
  settings: {
    colors: {
      primary: '#A880FF',
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

const gdGetStarted = (url: string) => `/v0.0.0-beta.13/getting-started${url}`;
const gdDocs = (url: string) => `/v0.0.0-beta.13/documentation${url}`;
const gdLearn = (url: string) => `/v0.0.0-beta.13/learning${url}`;

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

export default defineTheme([
  site('main', {
    extends: [template],
    directories: [
      directory('main', {
        sidebar: [],
      }),
    ],
  }),
  site('v0.0.0-beta.13', {
    extends: [template],
    contentFooter: {
      text: 'Copyright © 2025',
    },
    tabs: [
      link('Getting started', '/v0.0.0-beta.13/getting-started'),
      link('Guides', '/v0.0.0-beta.13/docs'),
      link('Learning', '/v0.0.0-beta.13/learn'),
    ],
    directories: [
      directory('getting-started', {
        sidebar: [
          ...starLinks,
          group('Getting started', [
            link(
              'Installation',
              gdGetStarted('/getting-started/installation'),
              {
                icon: 'fa6-solid:download',
              },
            ),
            link('Development', gdGetStarted('/getting-started/development'), {
              icon: 'fa6-solid:code',
            }),
          ]),
          group('Configuration', [
            link('Event storage', gdGetStarted('/config/eventstorage'), {
              icon: 'fa6-solid:database',
            }),
            link('Projections', gdGetStarted('/config/projections'), {
              icon: 'icon-park-solid:data',
            }),
            link('Cheers!!', gdGetStarted('/config/cheers'), {
              icon: 'icon-park-solid:success',
            }),
          ]),
          group('Advanced', [
            link('Upcoming', gdGetStarted('/advanced/not-yet')),
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
  site('v0.0.0-beta.12', {
    extends: [template],
    directories: [
      directory('main', {
        sidebar: [...starLinks],
      }),
    ],
  }),
]) satisfies SiteComponent[];
