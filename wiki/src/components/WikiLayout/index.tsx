import React from 'react';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import {useLocation} from '@docusaurus/router';

const navItems = [
  {to: '/resources', label: 'Resources'},
  {to: '/methods', label: 'Methods'},
];

const sidebarStyle: React.CSSProperties = {
  width: '200px',
  flexShrink: 0,
  borderRight: '1px solid var(--ifm-toc-border-color)',
  padding: '1.5rem 0.75rem',
  alignSelf: 'stretch',
};

const sectionLabelStyle: React.CSSProperties = {
  fontWeight: 700,
  fontSize: '0.75em',
  textTransform: 'uppercase',
  letterSpacing: '0.08em',
  opacity: 0.5,
  padding: '0 0.75rem',
  marginBottom: '0.5rem',
};

export default function WikiLayout({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}): JSX.Element {
  const {pathname} = useLocation();

  return (
    <Layout title={title}>
      <div style={{display: 'flex', flex: 1}}>
        <aside style={sidebarStyle}>
          <div style={sectionLabelStyle}>Wiki</div>
          <nav>
            {navItems.map((item) => {
              const active =
                pathname === item.to || pathname.startsWith(item.to + '/');
              return (
                <div key={item.to}>
                  <Link
                    to={item.to}
                    style={{
                      display: 'block',
                      padding: '0.4rem 0.75rem',
                      borderRadius: '4px',
                      fontWeight: active ? 600 : 400,
                      background: active
                        ? 'var(--ifm-color-emphasis-200)'
                        : 'transparent',
                      color: 'inherit',
                      textDecoration: 'none',
                    }}
                  >
                    {item.label}
                  </Link>
                </div>
              );
            })}
          </nav>
        </aside>
        <div style={{flex: 1, minWidth: 0}}>
          {children}
        </div>
      </div>
    </Layout>
  );
}
