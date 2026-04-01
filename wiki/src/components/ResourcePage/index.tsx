import React from 'react';
import Link from '@docusaurus/Link';
import useBaseUrl from '@docusaurus/useBaseUrl';
import WikiLayout from '@site/src/components/WikiLayout';

type Resource = {
  id: string;
  Name: string;
  Category: string;
  Stage: string;
  Description?: string;
};

type RecipeSlot = {id: string; count: number};
type Recipe = {id: string; Method?: string; input: RecipeSlot[]; output: RecipeSlot[]};
type ResourceMap = Record<string, {id: string; Name: string; imageFile: string}>;
type Method = {id: string; name: string; industry: string; description: string};
type MethodMap = Record<string, Method>;

interface PageData {
  resource: Resource;
  producedBy: Recipe[];
  usedIn: Recipe[];
  resourceMap: ResourceMap;
  methodMap: MethodMap;
  imageFile: string;
}

interface Props {
  pageData: PageData;
}

function Slot({
  slot,
  resourceMap,
}: {
  slot: RecipeSlot;
  resourceMap: ResourceMap;
}): JSX.Element {
  const entry = resourceMap[slot.id];
  const name = entry?.Name ?? slot.id;
  const suffix = ` ×${slot.count}`;

  if (entry) {
    return (
      <>
        <Link to={`/resources/${slot.id}`}>{name}</Link>
        {suffix}
      </>
    );
  }
  return <>{name}{suffix}</>;
}

function SlotList({slots, resourceMap}: {slots: RecipeSlot[]; resourceMap: ResourceMap}): JSX.Element {
  const multi = slots.length > 1;
  return (
    <>
      {slots.map((slot, i) => (
        <React.Fragment key={slot.id}>
          {i > 0 && <br />}
          {multi && <span style={{userSelect: 'none', marginRight: '0.3em'}}>–</span>}
          <Slot slot={slot} resourceMap={resourceMap} />
        </React.Fragment>
      ))}
    </>
  );
}

function MethodCell({methodId, methodMap}: {methodId: string | undefined; methodMap: MethodMap}): JSX.Element {
  if (!methodId) return <>—</>;
  const method = methodMap[methodId];
  const label = method?.name ?? methodId;
  return <Link to={`/methods#${methodId}`}>{label}</Link>;
}

function ProducedByTable({recipes, resourceMap, methodMap}: {recipes: Recipe[]; resourceMap: ResourceMap; methodMap: MethodMap}): JSX.Element {
  return (
    <table>
      <thead>
        <tr><th>Method</th><th>Input</th><th>Output</th></tr>
      </thead>
      <tbody>
        {recipes.map((recipe) => (
          <tr key={recipe.id}>
            <td><MethodCell methodId={recipe.Method} methodMap={methodMap} /></td>
            <td><SlotList slots={recipe.input} resourceMap={resourceMap} /></td>
            <td><SlotList slots={recipe.output} resourceMap={resourceMap} /></td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function UsedInTable({recipes, resourceMap, methodMap}: {recipes: Recipe[]; resourceMap: ResourceMap; methodMap: MethodMap}): JSX.Element {
  const baseImgUrl = useBaseUrl('img/resources/thumb/');
  return (
    <table>
      <thead>
        <tr><th></th><th>Output</th><th>Input</th><th>Method</th></tr>
      </thead>
      <tbody>
        {recipes.map((recipe) => {
          const firstOutput = recipe.output[0];
          const imgFile = firstOutput ? (resourceMap[firstOutput.id]?.imageFile ?? 'generic.png') : 'generic.png';
          return (
            <tr key={recipe.id}>
              <td><img src={`${baseImgUrl}${imgFile}`} alt="" width={32} height={32} /></td>
              <td><SlotList slots={recipe.output} resourceMap={resourceMap} /></td>
              <td><SlotList slots={recipe.input} resourceMap={resourceMap} /></td>
              <td><MethodCell methodId={recipe.Method} methodMap={methodMap} /></td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

const badgeStyle: React.CSSProperties = {
  display: 'inline-block',
  background: 'var(--ifm-color-emphasis-200)',
  borderRadius: '4px',
  padding: '2px 8px',
  marginRight: '8px',
  fontSize: '0.85em',
};

export default function ResourcePage({pageData}: Props): JSX.Element {
  const {resource, producedBy, usedIn, resourceMap, methodMap, imageFile} = pageData;
  const imgSrc = useBaseUrl(`img/resources/${imageFile}`);

  return (
    <WikiLayout title={resource.Name}>
      <main style={{padding: '2rem', maxWidth: '800px'}}>
        <div style={{display: 'flex', gap: '2rem', alignItems: 'flex-start', marginBottom: '1.5rem'}}>
          <img
            src={imgSrc}
            alt={resource.Name}
            width={128}
            height={128}
            style={{flexShrink: 0}}
          />
          <div>
            <h1 style={{marginTop: 0}}>{resource.Name}</h1>
            <p style={{margin: 0}}>
              <span style={badgeStyle}>{resource.Category.replace(/-/g, ' ')}</span>
              <span style={badgeStyle}>{resource.Stage}</span>
            </p>
          </div>
        </div>

        {resource.Description && <p>{resource.Description}</p>}

        <h2>Produced by</h2>
        {producedBy.length === 0 ? (
          <p><em>None — this is a primary resource.</em></p>
        ) : (
          <ProducedByTable recipes={producedBy} resourceMap={resourceMap} methodMap={methodMap} />
        )}

        <h2>Used in</h2>
        {usedIn.length === 0 ? (
          <p><em>None — this resource is not used in any recipe.</em></p>
        ) : (
          <UsedInTable recipes={usedIn} resourceMap={resourceMap} methodMap={methodMap} />
        )}
      </main>
    </WikiLayout>
  );
}
