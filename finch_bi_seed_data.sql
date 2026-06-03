-- =============================================================
--  Finch BI — Seed Data
--  Matches the data model in _RNPVKJC.html exactly.
--  Reference date: 26 May 2026 (the dashboard's "today").
--  Run these in order: schema → lookups → core tables → indexes.
-- =============================================================


-- =============================================================
--  SCHEMA
-- =============================================================

CREATE TABLE IF NOT EXISTS segments (
  id          VARCHAR(20)  PRIMARY KEY,        -- 'major','mid','grass','recurring'
  label       VARCHAR(60)  NOT NULL,
  avg_gift    INT          NOT NULL,           -- typical avg gift size used for sizing
  donor_count INT          NOT NULL,           -- baseline pool count
  color       VARCHAR(10)  NOT NULL,
  icon        VARCHAR(40)  NOT NULL
);

CREATE TABLE IF NOT EXISTS campaigns (
  id          VARCHAR(20)  PRIMARY KEY,        -- 'spring','monthly','major','eoy','capital'
  label       VARCHAR(60)  NOT NULL,
  color       VARCHAR(10)  NOT NULL,
  goal        INT          NOT NULL            -- FY26 campaign fundraising goal (USD)
);

CREATE TABLE IF NOT EXISTS channels (
  id          VARCHAR(20)  PRIMARY KEY,        -- 'online','phone','mail','event'
  label       VARCHAR(40)  NOT NULL,
  color       VARCHAR(10)  NOT NULL,
  base_pct    INT          NOT NULL            -- approximate % share of total volume
);

CREATE TABLE IF NOT EXISTS donors (
  name        VARCHAR(80)  PRIMARY KEY,
  segment_id  VARCHAR(20)  NOT NULL REFERENCES segments(id),
  max_gift    INT          NOT NULL,           -- donor's typical largest single gift
  first_gift  VARCHAR(10)  NOT NULL,           -- 'Mon YYYY' string matching the dashboard display
  lifecycle   VARCHAR(20)  NOT NULL            -- 'active','new','lapsed','reactivated'
);

CREATE TABLE IF NOT EXISTS callers (
  name        VARCHAR(80)  PRIMARY KEY,
  role        VARCHAR(30)  NOT NULL,           -- 'Senior caller','Caller II','Caller I','Part-time','Trainee'
  tenure      VARCHAR(10)  NOT NULL,           -- e.g. '4y','10mo','3mo'
  conv_boost  DECIMAL(4,2) NOT NULL            -- multiplier on base conversion probability
);

CREATE TABLE IF NOT EXISTS donations (
  id          SERIAL       PRIMARY KEY,
  gift_date   DATE         NOT NULL,
  donor_name  VARCHAR(80)  NOT NULL REFERENCES donors(name),
  campaign_id VARCHAR(20)  NOT NULL REFERENCES campaigns(id),
  channel_id  VARCHAR(20)  NOT NULL REFERENCES channels(id),
  amount      INT          NOT NULL,
  status      VARCHAR(10)  NOT NULL            -- 'completed','pending','failed'
);

CREATE TABLE IF NOT EXISTS calls (
  id          SERIAL       PRIMARY KEY,
  call_time   TIMESTAMP    NOT NULL,
  caller_name VARCHAR(80)  NOT NULL REFERENCES callers(name),
  contact     VARCHAR(80)  NOT NULL REFERENCES donors(name),
  duration_sec INT         NOT NULL,           -- 0 for missed/voicemail
  outcome     VARCHAR(12)  NOT NULL,           -- 'answered','voicemail','missed'
  pledge      INT          NOT NULL DEFAULT 0,
  note_text   TEXT,                            -- captured donor impact quote (nullable)
  note_context VARCHAR(80)                     -- quote context label (nullable)
);


-- =============================================================
--  LOOKUP TABLES
-- =============================================================

-- Segments
INSERT INTO segments (id, label, avg_gift, donor_count, color, icon) VALUES
  ('major',     'Major ($1K+)',            3530, 42,  '#7F77DD', 'ti-crown'),
  ('mid',       'Mid-level ($250-999)',     605, 118, '#185FA5', 'ti-star'),
  ('grass',     'Grassroots (<$250)',        80, 684, '#1D9E75', 'ti-users'),
  ('recurring', 'Recurring (monthly)',      200, 312, '#0F6E56', 'ti-repeat');

-- Campaigns
INSERT INTO campaigns (id, label, color, goal) VALUES
  ('spring',  'Spring appeal',     '#0F6E56', 20000),
  ('monthly', 'Monthly giving',    '#185FA5', 18000),
  ('major',   'Major gifts',       '#7F77DD', 75000),
  ('eoy',     'End-of-year',       '#EF9F27', 40000),
  ('capital', 'Capital campaign',  '#D4537E', 45000);

-- Channels
INSERT INTO channels (id, label, color, base_pct) VALUES
  ('online', 'Online', '#0F6E56', 52),
  ('phone',  'Phone',  '#185FA5', 24),
  ('mail',   'Mail',   '#7F77DD', 14),
  ('event',  'Event',  '#EF9F27', 10);


-- =============================================================
--  DONORS  (55 donors across 4 segments)
-- =============================================================

INSERT INTO donors (name, segment_id, max_gift, first_gift, lifecycle) VALUES

  -- Major donors ($1K+)
  ('Hartwell Foundation',  'major', 18500, 'Mar 2019', 'active'),
  ('Aspen Family Trust',   'major',  9800, 'Sep 2021', 'active'),
  ('Boulder Trust',        'major',  5800, 'Jan 2020', 'active'),
  ('Coronado Group',       'major',  7200, 'May 2022', 'active'),
  ('Cherry Creek Trust',   'major',  6400, 'Aug 2023', 'new'),
  ('Westwood Foundation',  'major',  4500, 'Aug 2020', 'lapsed'),
  ('Wilkins Estate',       'major',  3800, 'Feb 2019', 'lapsed'),
  ('Berkeley Trust',       'major',  5200, 'Nov 2021', 'reactivated'),

  -- Mid-level ($250–999)
  ('M. Chen',      'mid', 1500, 'Feb 2018', 'active'),
  ('R. Patel',     'mid',  900, 'Aug 2020', 'active'),
  ('D. Okafor',    'mid',  850, 'Nov 2019', 'active'),
  ('S. Nakamura',  'mid',  650, 'Apr 2022', 'active'),
  ('K. Thornton',  'mid',  750, 'Jun 2021', 'reactivated'),
  ('J. Liu',       'mid',  500, 'Sep 2023', 'active'),
  ('H. Mwangi',    'mid',  625, 'Feb 2024', 'new'),
  ('A. Vasquez',   'mid',  550, 'Feb 2020', 'lapsed'),
  ('R. Goldman',   'mid',  880, 'Jul 2019', 'lapsed'),
  ('P. Saunders',  'mid',  420, 'Oct 2021', 'lapsed'),
  ('D. Kim',       'mid',  680, 'Mar 2020', 'lapsed'),
  ('M. Atkinson',  'mid',  540, 'May 2022', 'lapsed'),
  ('F. Olsen',     'mid',  395, 'Aug 2018', 'lapsed'),

  -- Grassroots (<$250)
  ('L. Garcia',    'grass', 300, 'Oct 2017', 'active'),
  ('A. Bekele',    'grass', 240, 'Jun 2023', 'active'),
  ('J. Thompson',  'grass', 280, 'Mar 2024', 'new'),
  ('P. Singh',     'grass', 120, 'Nov 2023', 'active'),
  ('E. Johansson', 'grass', 135, 'Jan 2024', 'new'),
  ('T. Brennan',   'grass',  80, 'Feb 2024', 'new'),
  ('O. Ngo',       'grass',  90, 'Apr 2024', 'new'),
  ('C. Foster',    'grass', 165, 'Sep 2023', 'active'),
  ('K. Bauer',     'grass', 115, 'Dec 2023', 'new'),
  ('I. Petrov',    'grass', 185, 'Jul 2022', 'reactivated'),
  ('M. Reyes',     'grass', 145, 'May 2023', 'lapsed'),
  ('D. Bartlett',  'grass', 130, 'Aug 2021', 'lapsed'),
  ('G. Antonov',   'grass', 170, 'Mar 2020', 'lapsed'),
  ('S. Patel',     'grass', 110, 'Nov 2020', 'lapsed'),
  ('A. Carter',    'grass',  75, 'Jun 2022', 'lapsed'),
  ('L. Tanaka',    'grass', 240, 'Jan 2019', 'lapsed'),
  ('B. Murphy',    'grass',  95, 'Sep 2021', 'lapsed'),
  ('N. Sanchez',   'grass', 210, 'Apr 2020', 'lapsed'),
  ('W. Hsu',       'grass', 120, 'Feb 2022', 'lapsed'),
  ('C. Romano',    'grass',  85, 'Oct 2020', 'lapsed'),
  ('V. Iyer',      'grass', 160, 'May 2021', 'lapsed'),
  ('R. Webb',      'grass',  65, 'Aug 2022', 'lapsed'),
  ('Anonymous',    'grass',  85, 'Aug 2024', 'new'),

  -- Recurring (monthly) — max = annual recurring total
  ('V. Chowdhury', 'recurring', 1200, 'Jul 2022', 'active'),
  ('C. Nakata',    'recurring',  900, 'Jan 2023', 'active'),
  ('B. Henderson', 'recurring',  750, 'Dec 2021', 'active'),
  ('R. Espinoza',  'recurring', 1080, 'Jun 2022', 'active'),
  ('Y. Ahmed',     'recurring',  600, 'Sep 2023', 'active'),
  ('F. Park',      'recurring',  840, 'Mar 2022', 'active'),
  ('N. Kowalski',  'recurring',  480, 'Aug 2023', 'reactivated'),
  ('T. Williams',  'recurring',  720, 'Nov 2022', 'active'),
  ('E. Ortega',    'recurring',  540, 'Feb 2024', 'new'),
  ('H. Diaz',      'recurring',  780, 'Sep 2020', 'lapsed'),
  ('M. Sato',      'recurring',  540, 'Mar 2021', 'lapsed');


-- =============================================================
--  CALLERS  (18-person call team)
-- =============================================================

INSERT INTO callers (name, role, tenure, conv_boost) VALUES
  -- Senior leads
  ('Maya Rodriguez',  'Senior caller', '4y',   1.20),
  ('Aisha Williams',  'Senior caller', '3y',   1.18),
  -- Veterans
  ('James Park',      'Caller II',     '2y',   1.05),
  ('Priya Desai',     'Caller II',     '2y',   1.08),
  ('Daniel Cohen',    'Caller II',     '2y',   1.02),
  -- Mid-tier
  ('Tom Schaefer',    'Caller I',      '1.5y', 0.92),
  ('Sofia Martinez',  'Caller I',      '1y',   0.98),
  ('Kenji Tanaka',    'Caller I',      '1y',   0.95),
  ('Rachel Brooks',   'Caller I',      '1y',   1.00),
  ('Marcus Lee',      'Caller I',      '1y',   0.90),
  ('Naomi Ferreira',  'Caller I',      '10mo', 0.94),
  -- Part-timers
  ('Aaron Goldberg',  'Part-time',     '2y',   1.04),
  ('Lena Vasiliev',   'Part-time',     '1y',   0.96),
  ('Omar Khalil',     'Part-time',     '8mo',  0.88),
  ('Hannah Schultz',  'Part-time',     '6mo',  0.85),
  -- Trainees / new hires
  ('Carlos Mendez',   'Trainee',       '3mo',  0.72),
  ('Jada Powell',     'Trainee',       '2mo',  0.68),
  ('Ethan Reilly',    'Trainee',       '1mo',  0.62);


-- =============================================================
--  SAMPLE DONATIONS
--  Covers FY26 YTD (Jan–26 May 2026).
--  Distribution follows the dashboard logic:
--    - Major donors: 4 gifts/year, heavy major/capital campaigns
--    - Mid: 3 gifts/year, spring/eoy
--    - Recurring: ~monthly via 'monthly' campaign, online
--    - Grassroots: 1–2 gifts, spring/eoy, mostly online
--  ~90 % completed, ~7% pending (very recent), ~3% failed.
-- =============================================================

INSERT INTO donations (gift_date, donor_name, campaign_id, channel_id, amount, status) VALUES

  -- === MAJOR DONORS ===
  -- Hartwell Foundation (active, max 18500)
  ('2026-01-15', 'Hartwell Foundation', 'major',   'phone', 5200, 'completed'),
  ('2026-02-28', 'Hartwell Foundation', 'capital',  'mail', 4100, 'completed'),
  ('2026-04-10', 'Hartwell Foundation', 'major',   'event', 3800, 'completed'),
  ('2026-05-18', 'Hartwell Foundation', 'eoy',     'phone', 2900, 'completed'),

  -- Aspen Family Trust (active, max 9800)
  ('2026-01-22', 'Aspen Family Trust',  'major',   'phone', 2800, 'completed'),
  ('2026-03-14', 'Aspen Family Trust',  'capital',  'mail', 2100, 'completed'),
  ('2026-05-02', 'Aspen Family Trust',  'major',   'event', 1900, 'completed'),

  -- Boulder Trust (active, max 5800)
  ('2026-02-05', 'Boulder Trust',       'major',   'phone', 1600, 'completed'),
  ('2026-03-30', 'Boulder Trust',       'capital',  'mail', 1400, 'completed'),
  ('2026-05-12', 'Boulder Trust',       'eoy',     'phone',  900, 'completed'),

  -- Coronado Group (active, max 7200)
  ('2026-01-08', 'Coronado Group',      'capital', 'event', 2000, 'completed'),
  ('2026-03-22', 'Coronado Group',      'major',   'phone', 1800, 'completed'),
  ('2026-05-08', 'Coronado Group',      'capital',  'mail', 1400, 'completed'),

  -- Cherry Creek Trust (new, max 6400)
  ('2026-04-18', 'Cherry Creek Trust',  'major',   'phone', 1700, 'completed'),
  ('2026-05-20', 'Cherry Creek Trust',  'capital',  'mail', 1400, 'pending'),

  -- Berkeley Trust (reactivated, max 5200)
  ('2026-03-05', 'Berkeley Trust',      'major',   'phone', 1500, 'completed'),
  ('2026-05-14', 'Berkeley Trust',      'spring',  'online', 900, 'completed'),

  -- Westwood Foundation (lapsed — rare comeback)
  -- (intentionally no current-period gift — lapsed)

  -- Wilkins Estate (lapsed)
  -- (intentionally no current-period gift — lapsed)

  -- === MID-LEVEL DONORS ===
  -- M. Chen (active, max 1500)
  ('2026-01-30', 'M. Chen',     'spring', 'online',  600, 'completed'),
  ('2026-03-25', 'M. Chen',     'eoy',    'online',  700, 'completed'),
  ('2026-05-15', 'M. Chen',     'major',  'phone',   500, 'completed'),

  -- R. Patel (active, max 900)
  ('2026-02-14', 'R. Patel',    'spring', 'online',  400, 'completed'),
  ('2026-04-20', 'R. Patel',    'eoy',    'online',  350, 'completed'),

  -- D. Okafor (active, max 850)
  ('2026-01-25', 'D. Okafor',   'eoy',    'phone',   380, 'completed'),
  ('2026-04-05', 'D. Okafor',   'spring', 'online',  320, 'completed'),

  -- S. Nakamura (active, max 650)
  ('2026-02-20', 'S. Nakamura', 'spring', 'mail',    280, 'completed'),
  ('2026-05-10', 'S. Nakamura', 'eoy',    'online',  260, 'completed'),

  -- K. Thornton (reactivated, max 750)
  ('2026-03-18', 'K. Thornton', 'eoy',    'phone',   450, 'completed'),

  -- J. Liu (active, max 500)
  ('2026-02-10', 'J. Liu',      'spring', 'online',  200, 'completed'),
  ('2026-04-28', 'J. Liu',      'eoy',    'online',  180, 'completed'),

  -- H. Mwangi (new, max 625)
  ('2026-04-15', 'H. Mwangi',   'spring', 'online',  300, 'completed'),
  ('2026-05-22', 'H. Mwangi',   'eoy',    'phone',   250, 'pending'),

  -- Lapsed mid-level: no current gifts (A. Vasquez, R. Goldman, P. Saunders, D. Kim, M. Atkinson, F. Olsen)

  -- === GRASSROOTS DONORS ===
  -- L. Garcia (active, max 300)
  ('2026-01-18', 'L. Garcia',    'spring', 'online',  100, 'completed'),
  ('2026-03-28', 'L. Garcia',    'eoy',    'online',   75, 'completed'),
  ('2026-05-18', 'L. Garcia',    'spring', 'online',   50, 'completed'),

  -- A. Bekele (active, max 240)
  ('2026-02-22', 'A. Bekele',    'eoy',    'online',  100, 'completed'),
  ('2026-05-05', 'A. Bekele',    'spring', 'online',   75, 'completed'),

  -- J. Thompson (new, max 280)
  ('2026-04-22', 'J. Thompson',  'spring', 'online',  100, 'completed'),

  -- P. Singh (active, max 120)
  ('2026-03-10', 'P. Singh',     'spring', 'online',   50, 'completed'),
  ('2026-05-16', 'P. Singh',     'eoy',    'online',   50, 'completed'),

  -- E. Johansson (new, max 135)
  ('2026-04-28', 'E. Johansson', 'spring', 'online',   75, 'completed'),

  -- T. Brennan (new, max 80)
  ('2026-05-10', 'T. Brennan',   'spring', 'online',   25, 'completed'),

  -- O. Ngo (new, max 90)
  ('2026-05-18', 'O. Ngo',       'spring', 'online',   50, 'pending'),

  -- C. Foster (active, max 165)
  ('2026-02-15', 'C. Foster',    'spring', 'online',   75, 'completed'),
  ('2026-04-30', 'C. Foster',    'eoy',    'online',   50, 'completed'),

  -- K. Bauer (new, max 115)
  ('2026-05-12', 'K. Bauer',     'spring', 'online',   50, 'completed'),

  -- I. Petrov (reactivated, max 185)
  ('2026-03-20', 'I. Petrov',    'spring', 'online',  100, 'completed'),

  -- Anonymous (new, max 85)
  ('2026-05-20', 'Anonymous',    'spring', 'online',   25, 'completed'),

  -- === RECURRING DONORS (monthly via 'monthly' campaign, online) ===
  -- V. Chowdhury ($120/mo)
  ('2026-01-01', 'V. Chowdhury', 'monthly', 'online', 120, 'completed'),
  ('2026-02-01', 'V. Chowdhury', 'monthly', 'online', 120, 'completed'),
  ('2026-03-01', 'V. Chowdhury', 'monthly', 'online', 120, 'completed'),
  ('2026-04-01', 'V. Chowdhury', 'monthly', 'online', 120, 'completed'),
  ('2026-05-01', 'V. Chowdhury', 'monthly', 'online', 120, 'completed'),

  -- C. Nakata ($90/mo)
  ('2026-01-05', 'C. Nakata',    'monthly', 'online',  90, 'completed'),
  ('2026-02-05', 'C. Nakata',    'monthly', 'online',  90, 'completed'),
  ('2026-03-05', 'C. Nakata',    'monthly', 'online',  90, 'completed'),
  ('2026-04-05', 'C. Nakata',    'monthly', 'online',  90, 'completed'),
  ('2026-05-05', 'C. Nakata',    'monthly', 'online',  90, 'completed'),

  -- B. Henderson ($75/mo)
  ('2026-01-10', 'B. Henderson', 'monthly', 'online',  75, 'completed'),
  ('2026-02-10', 'B. Henderson', 'monthly', 'online',  75, 'completed'),
  ('2026-03-10', 'B. Henderson', 'monthly', 'online',  75, 'completed'),
  ('2026-04-10', 'B. Henderson', 'monthly', 'online',  75, 'completed'),
  ('2026-05-10', 'B. Henderson', 'monthly', 'online',  75, 'completed'),

  -- R. Espinoza ($108/mo)
  ('2026-01-12', 'R. Espinoza',  'monthly', 'online', 108, 'completed'),
  ('2026-02-12', 'R. Espinoza',  'monthly', 'online', 108, 'completed'),
  ('2026-03-12', 'R. Espinoza',  'monthly', 'online', 108, 'completed'),
  ('2026-04-12', 'R. Espinoza',  'monthly', 'online', 108, 'completed'),
  ('2026-05-12', 'R. Espinoza',  'monthly', 'online', 108, 'completed'),

  -- Y. Ahmed ($60/mo)
  ('2026-01-15', 'Y. Ahmed',     'monthly', 'online',  60, 'completed'),
  ('2026-02-15', 'Y. Ahmed',     'monthly', 'online',  60, 'completed'),
  ('2026-03-15', 'Y. Ahmed',     'monthly', 'online',  60, 'completed'),
  ('2026-04-15', 'Y. Ahmed',     'monthly', 'online',  60, 'completed'),
  ('2026-05-15', 'Y. Ahmed',     'monthly', 'online',  60, 'completed'),

  -- F. Park ($84/mo)
  ('2026-01-18', 'F. Park',      'monthly', 'online',  84, 'completed'),
  ('2026-02-18', 'F. Park',      'monthly', 'online',  84, 'completed'),
  ('2026-03-18', 'F. Park',      'monthly', 'online',  84, 'completed'),
  ('2026-04-18', 'F. Park',      'monthly', 'online',  84, 'completed'),
  ('2026-05-18', 'F. Park',      'monthly', 'online',  84, 'completed'),

  -- N. Kowalski ($48/mo, reactivated)
  ('2026-03-22', 'N. Kowalski',  'monthly', 'online',  48, 'completed'),
  ('2026-04-22', 'N. Kowalski',  'monthly', 'online',  48, 'completed'),
  ('2026-05-22', 'N. Kowalski',  'monthly', 'online',  48, 'completed'),

  -- T. Williams ($72/mo)
  ('2026-01-20', 'T. Williams',  'monthly', 'online',  72, 'completed'),
  ('2026-02-20', 'T. Williams',  'monthly', 'online',  72, 'completed'),
  ('2026-03-20', 'T. Williams',  'monthly', 'online',  72, 'completed'),
  ('2026-04-20', 'T. Williams',  'monthly', 'online',  72, 'completed'),
  ('2026-05-20', 'T. Williams',  'monthly', 'online',  72, 'completed'),

  -- E. Ortega ($54/mo, new)
  ('2026-03-25', 'E. Ortega',    'monthly', 'online',  54, 'completed'),
  ('2026-04-25', 'E. Ortega',    'monthly', 'online',  54, 'completed'),
  ('2026-05-25', 'E. Ortega',    'monthly', 'online',  54, 'pending');

  -- H. Diaz & M. Sato: lapsed recurring — no current gifts


-- =============================================================
--  SAMPLE CALLS  (~40 representative rows)
--  Outcomes: answered (with/without pledge), voicemail, missed.
--  Top callers (Maya, Aisha) have higher pledge rates.
-- =============================================================

INSERT INTO calls (call_time, caller_name, contact, duration_sec, outcome, pledge, note_text, note_context) VALUES

  -- Maya Rodriguez — senior, high conversion
  ('2026-05-24 10:15:00', 'Maya Rodriguez', 'Hartwell Foundation', 480, 'answered', 5200, 'We''ve supported the foundation for six years now. The program metrics you shared last quarter — that''s why I keep writing the check.', 'Annual stewardship call'),
  ('2026-05-23 11:30:00', 'Maya Rodriguez', 'M. Chen',             360, 'answered', 500,  'Tell Maya she''s the reason I gave again — last year''s call, she remembered my husband''s name and asked how he was doing. That''s stewardship.', 'Spring appeal pledge'),
  ('2026-05-22 14:00:00', 'Maya Rodriguez', 'V. Chowdhury',        290, 'answered', 0,    NULL, NULL),
  ('2026-05-20 09:45:00', 'Maya Rodriguez', 'A. Bekele',            45, 'voicemail', 0,   NULL, NULL),
  ('2026-05-18 13:15:00', 'Maya Rodriguez', 'K. Thornton',         420, 'answered', 450,  'I''d drifted away for a couple years — life got busy. Your caller reminded me why I started giving in the first place. I''m back.', 'Win-back call'),
  ('2026-05-15 10:00:00', 'Maya Rodriguez', 'Cherry Creek Trust',  510, 'answered', 1700, 'The capital campaign is exactly the kind of long-horizon work we want our family name attached to. Send me the recognition options.', 'Capital campaign pledge'),
  ('2026-05-12 11:00:00', 'Maya Rodriguez', 'L. Garcia',            20, 'missed',   0,    NULL, NULL),

  -- Aisha Williams — senior, high conversion
  ('2026-05-23 09:30:00', 'Aisha Williams', 'Aspen Family Trust',  540, 'answered', 2800, 'I want to be clear: we''re committing this gift because the leadership team has been transparent about challenges, not just wins. That earns trust.', 'Pledge confirmation'),
  ('2026-05-21 14:30:00', 'Aisha Williams', 'R. Patel',            300, 'answered', 400,  'You''re the only nonprofit that actually calls me with updates instead of just asking for more. That matters more than you know.', 'Renewal call'),
  ('2026-05-20 16:00:00', 'Aisha Williams', 'Berkeley Trust',      390, 'answered', 1500, 'Honestly forgot I''d lapsed. Glad you reached out. Process the same amount as before and add me to monthly.', 'Reactivation pledge'),
  ('2026-05-18 10:30:00', 'Aisha Williams', 'J. Thompson',          30, 'voicemail', 0,   NULL, NULL),
  ('2026-05-16 13:00:00', 'Aisha Williams', 'P. Singh',            240, 'answered', 50,   'I''m not wealthy, but $25 a month feels like something I can actually do. Thank you for making it easy.', 'New monthly donor'),
  ('2026-05-14 11:15:00', 'Aisha Williams', 'H. Mwangi',           330, 'answered', 300,  'Saw your work on social media, then your caller followed up with such warmth. I had to give.', 'First-time pledge'),

  -- James Park — veteran caller
  ('2026-05-22 10:00:00', 'James Park', 'Coronado Group',   450, 'answered', 2000, NULL, NULL),
  ('2026-05-20 14:00:00', 'James Park', 'S. Nakamura',       280, 'answered', 280,  NULL, NULL),
  ('2026-05-19 09:00:00', 'James Park', 'T. Williams',        15, 'missed',    0,   NULL, NULL),
  ('2026-05-18 15:30:00', 'James Park', 'R. Espinoza',       310, 'answered', 0,    NULL, NULL),
  ('2026-05-15 10:45:00', 'James Park', 'D. Okafor',         260, 'answered', 380,  'I lost my mom to the disease your org works on. Every gift I give is in her memory. That''s why I''ll never stop.', 'On increasing their pledge'),

  -- Priya Desai — veteran
  ('2026-05-21 11:00:00', 'Priya Desai', 'Boulder Trust',   400, 'answered', 1600, NULL, NULL),
  ('2026-05-19 13:30:00', 'Priya Desai', 'J. Liu',          200, 'answered', 200,  NULL, NULL),
  ('2026-05-17 10:00:00', 'Priya Desai', 'C. Foster',        38, 'voicemail', 0,   NULL, NULL),
  ('2026-05-15 14:00:00', 'Priya Desai', 'E. Johansson',    260, 'answered', 75,   'This is my first gift to anyone in a few years. The story your caller shared really hit me. I want to be part of this.', 'Acquisition call'),

  -- Daniel Cohen — veteran
  ('2026-05-20 09:15:00', 'Daniel Cohen', 'F. Park',         290, 'answered', 84,  'Bumping mine from $20 to $30 a month. Coffee costs more than that and means a lot less.', 'Sustainer upgrade'),
  ('2026-05-18 11:30:00', 'Daniel Cohen', 'N. Kowalski',     320, 'answered', 48,  'I lost my job last fall and had to pause. Now I''m back in. Don''t ever stop calling to check in — that kept me connected.', 'Reactivation call'),
  ('2026-05-16 14:15:00', 'Daniel Cohen', 'A. Vasquez',       18, 'missed',    0,  NULL, NULL),

  -- Tom Schaefer — mid-tier
  ('2026-05-22 10:30:00', 'Tom Schaefer', 'B. Henderson',    210, 'answered', 75,  NULL, NULL),
  ('2026-05-19 12:00:00', 'Tom Schaefer', 'Y. Ahmed',        240, 'answered', 60,  NULL, NULL),
  ('2026-05-17 09:30:00', 'Tom Schaefer', 'D. Kim',           40, 'voicemail', 0,  NULL, NULL),
  ('2026-05-14 14:30:00', 'Tom Schaefer', 'R. Goldman',       15, 'missed',    0,  NULL, NULL),

  -- Sofia Martinez — mid-tier
  ('2026-05-21 10:00:00', 'Sofia Martinez', 'C. Nakata',     260, 'answered', 90,  NULL, NULL),
  ('2026-05-19 14:00:00', 'Sofia Martinez', 'I. Petrov',     310, 'answered', 100, 'I appreciated that you didn''t lead with the ask. You asked how I was first. That''s why I''m giving again.', 'Returning donor'),
  ('2026-05-17 11:30:00', 'Sofia Martinez', 'M. Atkinson',    22, 'voicemail', 0,  NULL, NULL),

  -- Rachel Brooks — mid-tier
  ('2026-05-20 13:00:00', 'Rachel Brooks', 'T. Brennan',     180, 'answered', 25,  'I usually screen these calls. Yours was different — felt like a real conversation, not a pitch. Here''s my card.', 'Cold-to-pledge conversion'),
  ('2026-05-18 09:45:00', 'Rachel Brooks', 'A. Carter',       35, 'voicemail', 0,  NULL, NULL),
  ('2026-05-15 14:00:00', 'Rachel Brooks', 'E. Ortega',      270, 'answered', 54,  'Twelve years of monthly giving. At this point, it''s part of who I am, not something I do.', 'Loyalty milestone'),

  -- Marcus Lee
  ('2026-05-21 11:15:00', 'Marcus Lee', 'K. Bauer',    150, 'answered', 50,  NULL, NULL),
  ('2026-05-19 15:00:00', 'Marcus Lee', 'O. Ngo',      170, 'answered', 50,  NULL, NULL),
  ('2026-05-17 10:30:00', 'Marcus Lee', 'W. Hsu',       18, 'missed',    0,  NULL, NULL),

  -- Aaron Goldberg (part-time)
  ('2026-05-23 17:00:00', 'Aaron Goldberg', 'Westwood Foundation', 390, 'answered', 0,    NULL, NULL),
  ('2026-05-21 18:30:00', 'Aaron Goldberg', 'R. Webb',              20, 'voicemail', 0,   NULL, NULL),

  -- Carlos Mendez (trainee)
  ('2026-05-24 10:30:00', 'Carlos Mendez', 'M. Reyes',     100, 'answered', 0,   NULL, NULL),
  ('2026-05-22 14:15:00', 'Carlos Mendez', 'D. Bartlett',   17, 'missed',   0,   NULL, NULL),
  ('2026-05-20 11:00:00', 'Carlos Mendez', 'F. Olsen',      22, 'voicemail', 0,  NULL, NULL),

  -- Jada Powell (trainee)
  ('2026-05-23 13:00:00', 'Jada Powell', 'G. Antonov',   18, 'missed',    0,  NULL, NULL),
  ('2026-05-21 10:00:00', 'Jada Powell', 'S. Patel',     130, 'answered',  0,  NULL, NULL);


-- =============================================================
--  USEFUL INDEXES
-- =============================================================

CREATE INDEX IF NOT EXISTS idx_donations_date       ON donations(gift_date);
CREATE INDEX IF NOT EXISTS idx_donations_status     ON donations(status);
CREATE INDEX IF NOT EXISTS idx_donations_donor      ON donations(donor_name);
CREATE INDEX IF NOT EXISTS idx_donations_campaign   ON donations(campaign_id);
CREATE INDEX IF NOT EXISTS idx_donations_channel    ON donations(channel_id);
CREATE INDEX IF NOT EXISTS idx_calls_time           ON calls(call_time);
CREATE INDEX IF NOT EXISTS idx_calls_caller         ON calls(caller_name);
CREATE INDEX IF NOT EXISTS idx_calls_outcome        ON calls(outcome);
CREATE INDEX IF NOT EXISTS idx_donors_segment       ON donors(segment_id);
CREATE INDEX IF NOT EXISTS idx_donors_lifecycle     ON donors(lifecycle);


-- =============================================================
--  QUICK VERIFICATION QUERIES
-- =============================================================

-- Total raised YTD (should be in the $80K–130K range for YTD on this donor pool)
-- SELECT SUM(amount) AS total_raised FROM donations WHERE status = 'completed';

-- Donor count (active givers with at least one completed gift)
-- SELECT COUNT(DISTINCT donor_name) AS active_donors FROM donations WHERE status = 'completed';

-- Average gift size
-- SELECT ROUND(AVG(amount)) AS avg_gift FROM donations WHERE status = 'completed';

-- Donations by channel
-- SELECT channel_id, SUM(amount) AS revenue, COUNT(*) AS gifts
-- FROM donations WHERE status = 'completed'
-- GROUP BY channel_id ORDER BY revenue DESC;

-- Donations by campaign vs goal
-- SELECT d.campaign_id, c.label, c.goal,
--        SUM(d.amount) AS raised,
--        ROUND(SUM(d.amount) * 100.0 / c.goal, 1) AS pct_of_goal
-- FROM donations d JOIN campaigns c ON d.campaign_id = c.id
-- WHERE d.status = 'completed'
-- GROUP BY d.campaign_id, c.label, c.goal ORDER BY raised DESC;

-- Top donors by total given
-- SELECT donor_name, SUM(amount) AS total, COUNT(*) AS gifts
-- FROM donations WHERE status = 'completed'
-- GROUP BY donor_name ORDER BY total DESC LIMIT 10;

-- Donor lifecycle breakdown
-- SELECT lifecycle, COUNT(*) AS donors FROM donors GROUP BY lifecycle;

-- At-risk donors (lapsed, sorted by estimated lifetime value)
-- SELECT d.name, d.segment_id, d.first_gift, d.lifecycle,
--        COALESCE(SUM(dn.amount), 0) AS lifetime_value
-- FROM donors d
-- LEFT JOIN donations dn ON d.name = dn.donor_name AND dn.status = 'completed'
-- WHERE d.lifecycle = 'lapsed'
-- GROUP BY d.name, d.segment_id, d.first_gift, d.lifecycle
-- ORDER BY lifetime_value DESC;

-- Caller leaderboard
-- SELECT caller_name,
--        COUNT(*) AS calls_placed,
--        SUM(CASE WHEN outcome = 'answered' THEN 1 ELSE 0 END) AS connected,
--        SUM(CASE WHEN pledge > 0 THEN 1 ELSE 0 END) AS pledges,
--        SUM(pledge) AS total_pledged,
--        ROUND(SUM(CASE WHEN outcome='answered' THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 1) AS connect_rate_pct
-- FROM calls GROUP BY caller_name ORDER BY total_pledged DESC;

-- Call outcomes breakdown
-- SELECT outcome,
--        COUNT(*) AS calls,
--        ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 1) AS pct
-- FROM calls GROUP BY outcome ORDER BY calls DESC;
