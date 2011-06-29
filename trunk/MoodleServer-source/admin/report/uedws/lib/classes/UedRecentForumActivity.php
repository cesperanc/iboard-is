<?php
	/**
	 * Moodle UedRecentForumActivity Object
	 * 
	 * @author Cláudio Esperança
	 */
	class UedRecentForumActivity{
		/** @var int forumId */
		public $forumId;
		
		/** @var string forumName */
		public $forumName;
		
		/** @var int sectionNumber */
		public $sectionNumber;
		
		/** @var UedForumActivity[] activity */
		public $activity=array();
	}
	
	/**
	 * Moodle UedForumActivity Object to use on the above class
	 * 
	 * @author Cláudio Esperança
	 */
	class UedForumActivity{
		/** @var string discussionType */
		public $discussionType;
		
		/** @var int parentId */
		public $parentId;
		
		/** @var int discussionId */
		public $discussionId;
		
		/** @var int userId */
		public $userId;
		
		/** @var UedDate date */
		public $date;
		
		/** @var int postId */
		public $postId;
		
		/** @var string postSubject */
		public $postSubject;
		
		/** @var string postUrl */
		public $postUrl;
	}