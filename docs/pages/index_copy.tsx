import {
  Button,
  GuiderLayout,
  Hero,
  Card,
  CardGrid,
} from '@neato/guider/client';
import { Home } from '../components/home';
import { HomeCard, HomeCardContainer } from '../components/home-card';

interface Project {
  href: string;
  icon: string;
  title: string;
  description: string;
}

export default function LandingPage() {
  const projects: Project[] = [];

  return (
    <GuiderLayout meta={{ layout: 'page' }}>
      <Home.Container>
        <Home.Title>Event sourcing infrastructure made easy</Home.Title>
        <Home.Subtitle>
          Flexible but lightweight event sourcing infrastructure with event
          storage of choice.
        </Home.Subtitle>
        <Hero.Badge title="v1.0.0-beta.13" to="/docs">
          Just went out of beta!
        </Hero.Badge>
        <HomeCardContainer>
          <Button to="/v0.0.0-beta.13">Get started</Button>
          <Button
            to="https://github.com/eventstorage/eventstorage"
            type="secondary"
          >
            View on GitHub
          </Button>
        </HomeCardContainer>
      </Home.Container>
      <CardGrid>
        <Card icon="material-symbols:edit-document" title="Focus on writing">
          Effortlessly create beautiful documentation sites using Markdown or
          MDX files.
        </Card>
        <Card icon="mdi:puzzle" title="Unopinionated">
          Guider doesn't make assumptions about your site. Use it by itself or
          include it as part of a larger project.
        </Card>
        <Card icon="solar:pallete-2-bold" title="Themable to the core">
          Comes out the box with a ready-to-go theme, but can easily be made to
          look exactly like what you have envisioned.
        </Card>
      </CardGrid>
    </GuiderLayout>
  );
}
