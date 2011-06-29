<?php
/**
 * This file defines settings pages and external pages under the "webservices" category
 * 
 * @package		UEDWS
 * @subpackage	report_uedws
 * @version		2010.0
 * @copyright 	Learning Distance Unit {@link http://ued.ipleiria.pt}, Polytechnic Institute of Leiria
 * @author		Cláudio Esperança <cesperanc@gmail.com>
 * @license 	GNU GPL v3 or later {@link http://www.gnu.org/copyleft/gpl.html}
 */
if ($hassiteconfig) { // speedup for non-admins, add all caps used on this page
    $ADMIN->add('modules', new admin_category('uedws', get_string('uedws_title', 'report_uedws')));
    $ADMIN->add('uedws', new admin_externalpage('viewuedws', get_string('uedws_viewwebservices', 'report_uedws'),
    				"$CFG->wwwroot/$CFG->admin/report/uedws/index.php?action=view&sesskey={$USER->sesskey}",
    				'moodle/site:config'));
}
