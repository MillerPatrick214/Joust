// EXPECT: not flagged — @/ alias + node: builtin are auto-skipped.
import utils from '@/utils';
import { test } from 'node:test';
export { utils, test };
