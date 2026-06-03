-- =============================================================================
-- FINCHBI SUPABASE SCHEMA + SEED DATA
-- Paste this into Supabase → SQL Editor → New query → Run
-- =============================================================================

-- ─── Tables ──────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS customers (
    customer_id   UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name    VARCHAR(100)  NOT NULL,
    last_name     VARCHAR(100)  NOT NULL,
    email         VARCHAR(255)  NOT NULL UNIQUE,
    region        VARCHAR(50)   NOT NULL,
    city          VARCHAR(100),
    tier          VARCHAR(20)   DEFAULT 'Standard',
    created_at    TIMESTAMPTZ   DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS products (
    product_id      UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    product_name    VARCHAR(255)  NOT NULL,
    category        VARCHAR(100)  NOT NULL,
    sku             VARCHAR(50)   UNIQUE,
    unit_price      DECIMAL(10,2) NOT NULL,
    unit_cost       DECIMAL(10,2) NOT NULL,
    stock_quantity  INT           DEFAULT 0,
    reorder_point   INT           DEFAULT 50,
    is_active       BOOLEAN       DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS orders (
    order_id        UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID          NOT NULL REFERENCES customers(customer_id),
    order_date      DATE          NOT NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'PENDING',
    shipping_method VARCHAR(50),
    total_amount    DECIMAL(12,2) NOT NULL,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    created_at      TIMESTAMPTZ   DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS order_items (
    item_id     UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id    UUID          NOT NULL REFERENCES orders(order_id),
    product_id  UUID          NOT NULL REFERENCES products(product_id),
    quantity    INT           NOT NULL,
    unit_price  DECIMAL(10,2) NOT NULL,
    discount_pct DECIMAL(5,2) DEFAULT 0
);

-- ─── Indexes ─────────────────────────────────────────────────────────────────

CREATE INDEX IF NOT EXISTS idx_orders_date       ON orders(order_date);
CREATE INDEX IF NOT EXISTS idx_orders_customer   ON orders(customer_id);
CREATE INDEX IF NOT EXISTS idx_order_items_order ON order_items(order_id);

-- ─── Seed Products ────────────────────────────────────────────────────────────

INSERT INTO products (product_name, category, sku, unit_price, unit_cost, stock_quantity, reorder_point) VALUES
  ('UltraBook Pro 15"',       'Electronics', 'LAP-001', 1299.00,  820.00, 120, 30),
  ('UltraBook Air 13"',       'Electronics', 'LAP-002',  999.00,  610.00, 200, 50),
  ('Gaming Desktop X9',       'Electronics', 'DSK-001', 1799.00, 1100.00,   8, 20),
  ('Wireless Headphones BT',  'Electronics', 'AUD-001',  349.00,  180.00, 400, 80),
  ('4K Smart Monitor 27"',    'Electronics', 'MON-001',  599.00,  320.00,  14, 25),
  ('Ergonomic Office Chair',  'Furniture',   'FUR-001',  459.00,  210.00, 150, 40),
  ('Standing Desk Pro',       'Furniture',   'FUR-002',  699.00,  340.00,   9, 20),
  ('Bookshelf 5-Tier',        'Furniture',   'FUR-003',  229.00,   90.00, 300, 60),
  ('Running Shoes Elite',     'Apparel',     'APP-001',  129.00,   52.00, 500,100),
  ('Yoga Pants Pro',          'Apparel',     'APP-002',   79.00,   28.00, 700,150),
  ('Blender Pro 1200W',       'Appliances',  'APL-001',  149.00,   62.00, 180, 40),
  ('Air Fryer XL',            'Appliances',  'APL-002',  119.00,   48.00, 220, 50),
  ('Robot Vacuum Gen3',       'Appliances',  'APL-003',  399.00,  195.00,  18, 25),
  ('Vitamin C Complex',       'Health',      'HLT-001',   29.99,    8.00,1000,200),
  ('Protein Powder 5lb',      'Health',      'HLT-002',   59.99,   22.00, 400, 80),
  ('JavaScript Deep Dive',    'Books',       'BOK-001',   49.99,   12.00, 600,100),
  ('Data Science Handbook',   'Books',       'BOK-002',   44.99,   11.00, 500,100),
  ('Mindful Leadership',      'Books',       'BOK-003',   24.99,    6.00, 800,150)
ON CONFLICT (sku) DO NOTHING;

-- ─── Seed Customers ───────────────────────────────────────────────────────────

INSERT INTO customers (first_name, last_name, email, region, city, tier)
SELECT
    first_names[1 + (n % 10)]  AS first_name,
    last_names[1 + (n % 10)]   AS last_name,
    'user' || n || '@demo.example.com' AS email,
    regions[1 + (n % 5)]       AS region,
    cities[1 + (n % 10)]       AS city,
    CASE n % 10
        WHEN 0 THEN 'Platinum'
        WHEN 1 THEN 'Gold'
        WHEN 2 THEN 'Silver'
        ELSE 'Standard'
    END AS tier
FROM
    generate_series(1, 200) AS n,
    (SELECT
        ARRAY['James','Emma','Liam','Olivia','Noah','Ava','William','Sophia','Benjamin','Isabella'] AS first_names,
        ARRAY['Smith','Johnson','Williams','Brown','Jones','Garcia','Miller','Davis','Martinez','Anderson'] AS last_names,
        ARRAY['Northeast','Southeast','Midwest','West','International'] AS regions,
        ARRAY['New York','Los Angeles','Chicago','Houston','Phoenix','Philadelphia','San Antonio','San Diego','Dallas','San Jose'] AS cities
    ) AS lookups
ON CONFLICT (email) DO NOTHING;

-- ─── Seed Orders ──────────────────────────────────────────────────────────────

INSERT INTO orders (customer_id, order_date, status, shipping_method, total_amount, discount_amount)
SELECT
    c.customer_id,
    CURRENT_DATE - (random() * 730)::int,
    CASE (random() * 10)::int
        WHEN 0 THEN 'CANCELLED'
        WHEN 1 THEN 'PROCESSING'
        ELSE 'DELIVERED'
    END,
    CASE (random() * 3)::int
        WHEN 0 THEN 'STANDARD'
        WHEN 1 THEN 'EXPRESS'
        ELSE 'OVERNIGHT'
    END,
    ROUND((50 + random() * 1950)::numeric, 2),
    ROUND((random() * 50)::numeric, 2)
FROM customers c
CROSS JOIN generate_series(1, 8)
ORDER BY random()
LIMIT 1200;

-- ─── Seed Order Items ─────────────────────────────────────────────────────────

INSERT INTO order_items (order_id, product_id, quantity, unit_price, discount_pct)
SELECT
    o.order_id,
    p.product_id,
    1 + (random() * 3)::int,
    p.unit_price,
    CASE (random() * 5)::int WHEN 0 THEN 10 WHEN 1 THEN 5 ELSE 0 END
FROM orders o
CROSS JOIN LATERAL (
    SELECT product_id, unit_price FROM products ORDER BY random() LIMIT 2
) p
WHERE o.status != 'CANCELLED';

-- ─── Verify ──────────────────────────────────────────────────────────────────

SELECT
    (SELECT COUNT(*) FROM customers)   AS customers,
    (SELECT COUNT(*) FROM products)    AS products,
    (SELECT COUNT(*) FROM orders)      AS orders,
    (SELECT COUNT(*) FROM order_items) AS line_items;
