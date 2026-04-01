import React from 'react';
import Link from '@docusaurus/Link';
import WikiLayout from '@site/src/components/WikiLayout';

type Resource = {
  id: string;
  Name: string;
  Category: string;
  Stage: string;
  Description?: string;
};

interface Props {
  resources: Resource[];
}

export default function ResourceIndex({resources}: Props): JSX.Element {
  const byCategory: Record<string, Resource[]> = {};
  for (const r of resources) {
    (byCategory[r.Category] ??= []).push(r);
  }
  const categories = Object.keys(byCategory).sort();

  return (
    <WikiLayout title="Resources">
      <main style={{padding: '2rem'}}>
        <h1>Resources</h1>
        {categories.map((cat) => (
          <section key={cat} style={{marginBottom: '2rem'}}>
            <h2 style={{textTransform: 'capitalize'}}>
              {cat.replace(/-/g, ' ')}
            </h2>
            <ul>
              {byCategory[cat].map((r) => (
                <li key={r.id}>
                  <Link to={`/resources/${r.id}`}>{r.Name}</Link>
                  {' '}
                  <small style={{opacity: 0.6}}>({r.Stage})</small>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </main>
    </WikiLayout>
  );
}
