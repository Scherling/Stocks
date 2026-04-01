import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import BackendBanner from './components/BackendBanner';
import TradersPage from './pages/TradersPage';
import TraderDetailPage from './pages/TraderDetailPage';
import AssetTypesPage from './pages/AssetTypesPage';
import SellOrdersPage from './pages/SellOrdersPage';
import TradesPage from './pages/TradesPage';
import MarketPage from './pages/MarketPage';
import LedgerPage from './pages/LedgerPage';

export default function App() {
  return (
    <BrowserRouter>
      <BackendBanner />
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Navigate to="/traders" replace />} />
          <Route path="traders" element={<TradersPage />} />
          <Route path="traders/:id" element={<TraderDetailPage />} />
          <Route path="asset-types" element={<AssetTypesPage />} />
          <Route path="sell-orders" element={<SellOrdersPage />} />
          <Route path="trades" element={<TradesPage />} />
          <Route path="market" element={<MarketPage />} />
          <Route path="ledger" element={<LedgerPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
