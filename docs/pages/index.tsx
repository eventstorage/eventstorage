import {
  Button,
  Card,
  CardGrid,
  GuiderLayout,
  Hero,
} from '@neato/guider/client';
import { Home } from '../components/home';

export default function LandingPage() {
  return (
    <GuiderLayout meta={{ layout: 'page', site: 'main' }}>
      <Hero>
        <Hero.Badge title="v0.0.0-beta.16" to="https://github.com/eventstorage/eventstorage/releases">
          is currently stable release
        </Hero.Badge>
        <Hero.Title>Out of the box event storage infrastructure</Hero.Title>
        <Hero.Subtitle>
          Reliable enterprise-grade event sourced infrastructure with event store of choice.
        </Hero.Subtitle>
        <Hero.Actions>
          <Button to="/v0.0.0-beta.16">Get started</Button>
          <Button to="https://github.com/eventstorage/eventstorage" type="secondary">
          View on GitHub
          </Button>
        </Hero.Actions>
      </Hero>
      <CardGrid>
        <Card icon="fa6-solid:database" title="Event sourced infrastructure">
          Born out of event sourcing. eventstorage offers enterprise-grade event
          sourcing to run asynchronous fully event-driven apps.
        </Card>
        <Card icon="fa6-solid:code" title="Configuration of choice">
          es allows selecting event storage of choice and multi projection modes
          with high-performance Redis as projection source powered by innovative .NET.
        </Card>
        <Card icon="icon-park-solid:cpu" title="High-performance storage">
          Places no layer of ORM abstraction over event store clients resulting in simple storage
          operations. We denormalize and run lightning-fast plain SQL.
        </Card>
      </CardGrid>
    </GuiderLayout>
  );
}
