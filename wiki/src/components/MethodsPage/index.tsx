import React from 'react';
import WikiLayout from '@site/src/components/WikiLayout';

type Method = {id: string; name: string; industry: string; description: string};

interface Props {
  methods: Method[];
}

export default function MethodsPage({methods}: Props): JSX.Element {
  // Group methods by industry, preserving insertion order
  const grouped: Record<string, Method[]> = {};
  for (const method of methods) {
    (grouped[method.industry] ??= []).push(method);
  }

  return (
    <WikiLayout title="Methods">
      <main style={{padding: '2rem', maxWidth: '800px'}}>
        <h1>Production Methods</h1>
        <p>All manufacturing and processing methods used in the industrial economy, grouped by sector.</p>
        {Object.entries(grouped).map(([industry, items]) => (
          <section key={industry}>
            <h2>{industry}</h2>
            <dl>
              {items.map((method) => (
                <React.Fragment key={method.id}>
                  <dt id={method.id} style={{fontWeight: 'bold', marginTop: '0.75rem'}}>
                    {method.name}
                  </dt>
                  <dd style={{marginLeft: '1.5rem', marginTop: '0.25rem', color: 'var(--ifm-color-emphasis-700)'}}>
                    {method.description}
                  </dd>
                </React.Fragment>
              ))}
            </dl>
          </section>
        ))}
      </main>
    </WikiLayout>
  );
}
